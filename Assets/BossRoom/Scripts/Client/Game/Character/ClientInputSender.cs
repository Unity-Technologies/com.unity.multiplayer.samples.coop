using MLAPI;
using System;
using UnityEngine;

namespace BossRoom.Client
{
    /// <summary>
    /// Captures inputs for a character on a client and sends them to the server.
    /// </summary>
    [RequireComponent(typeof(NetworkCharacterState))]
    public class ClientInputSender : NetworkedBehaviour
    {
        private const float k_MouseInputRaycastDistance = 100f;

        // Cache raycast hit array so that we can use non alloc raycasts
        private readonly RaycastHit[] k_CachedHit = new RaycastHit[4];

        // This is basically a constant but layer masks cannot be created in the constructor, that's why it's assigned int Awake.
        private LayerMask k_GroundLayerMask;
        private LayerMask k_ActionLayerMask;

        private NetworkCharacterState m_NetworkCharacter;

        /// <summary>
        /// We detect clicks in Update (because you can miss single discrete clicks in FixedUpdate). But we need to
        /// raycast in FixedUpdate, because raycasts done in Update won't work reliably.
        /// This nullable vector will be set to a screen coordinate when an attack click was made.
        /// </summary>
        System.Nullable<Vector3> m_ClickRequest;
        bool m_Skill2Request;
        bool m_SkillActive = false;

        Camera m_MainCamera;

		ActionType m_EmoteAction;

        public event Action<Vector3> OnClientClick;

        /// <summary>
        /// Convenience getter that returns our CharacterData
        /// </summary>
        CharacterClass CharacterData => GameDataSource.Instance.CharacterDataByType[m_NetworkCharacter.CharacterType.Value];

        public override void NetworkStart()
        {
            // TODO Don't use NetworkedBehaviour for just NetworkStart [GOMPS-81]
            if (!IsClient || !IsOwner)
            {
                enabled = false;
            }

            k_GroundLayerMask = LayerMask.GetMask(new[] { "Ground" });
            k_ActionLayerMask = LayerMask.GetMask(new[] { "PCs", "NPCs", "Ground" });
        }

        void Awake()
        {
            m_NetworkCharacter = GetComponent<NetworkCharacterState>();
            m_MainCamera = Camera.main;
        }

        public void FinishSkill()
        {
            m_SkillActive = false;
        }

        void FixedUpdate()
        {
            // TODO replace with new Unity Input System [GOMPS-81]

            // The decision to block other inputs while a skill is active is up to debate, we can change this behaviour if needed
            if (m_SkillActive)
            {
                return;
            }
            if (m_Skill2Request)
            {
                var skill2 = Instantiate(GameDataSource.Instance.ActionDataByType[CharacterData.Skill2].ActionInput);
                skill2.Initiate(m_NetworkCharacter, CharacterData.Skill2, FinishSkill);
                m_SkillActive = true;
                m_Skill2Request = false;
                return;
            }
            // Is mouse button pressed (not just checking for down to allow continuous movement inputs by holding the mouse button down)
            if (Input.GetMouseButton(0))
            {
                var ray = m_MainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.RaycastNonAlloc(ray, k_CachedHit, k_MouseInputRaycastDistance, k_GroundLayerMask) > 0)
                {
                    // The MLAPI_INTERNAL channel is a reliable sequenced channel. Inputs should always arrive and be in order that's why this channel is used.
                    m_NetworkCharacter.InvokeServerRpc(m_NetworkCharacter.SendCharacterInputServerRpc, k_CachedHit[0].point,
                        "MLAPI_INTERNAL");
                    //Send our client only click request
                    OnClientClick?.Invoke(k_CachedHit[0].point);
                }
            }

            if (m_ClickRequest != null)
            {
                var ray = m_MainCamera.ScreenPointToRay(m_ClickRequest.Value);
                m_ClickRequest = null;

                int numHits = Physics.RaycastNonAlloc(ray, k_CachedHit, k_MouseInputRaycastDistance, k_ActionLayerMask);
                if (numHits == 0)
                {
                    return;
                }

                int networkedHitIndex = -1;
                for (int i = 0; i < numHits; i++)
                {
                    if (k_CachedHit[i].transform.GetComponent<NetworkedObject>())
                    {
                        networkedHitIndex = i;
                        break;
                    }
                }

                if (networkedHitIndex >= 0)
                {
                    if (GetActionRequestForTarget(ref k_CachedHit[networkedHitIndex], out ActionRequestData playerAction))
                    {
                        m_NetworkCharacter.ClientSendActionRequest(ref playerAction);
                    }
                }
                else
                {
                    // clicked on nothing... perform a "miss" attack on the spot they clicked on
                    var data = new ActionRequestData();
                    PopulateSkillRequest(k_CachedHit[0].point, CharacterData.Skill1, ref data);
                    m_NetworkCharacter.ClientSendActionRequest(ref data);
                }
            }
        }

        /// <summary>
        /// When you right-click on something you will want to do contextually different things. For example you might attack an enemy,
        /// but revive a friend. You might also decide to do nothing (e.g. right-clicking on a friend who hasn't FAINTED).
        /// </summary>
        /// <param name="hit">The RaycastHit of the entity we clicked on.</param>
        /// <param name="resultData">Out parameter that will be filled with the resulting action, if any.</param>
        /// <returns>true if we should play an action, false otherwise. </returns>
        private bool GetActionRequestForTarget(ref RaycastHit hit, out ActionRequestData resultData)
        {
            resultData = new ActionRequestData();
            var targetNetState = hit.transform.GetComponent<NetworkCharacterState>();
            if (targetNetState == null)
            {
                //Not a Character. In the future this could represent interacting with some other interactable, but for
                //now, it implies we just do nothing.
                return false;
            }

            if (targetNetState.IsNpc)
            {
                ActionType skill1 = CharacterData.Skill1;

                // record our target in case this action uses that info (non-targeted attacks will ignore this)
                resultData.TargetIds = new ulong[] { targetNetState.NetworkId };
                PopulateSkillRequest(hit.point, skill1, ref resultData);
                return true;
            }
            else if (targetNetState.NetworkLifeState.Value == LifeState.Fainted)
            {
                resultData = new ActionRequestData();
                resultData.TargetIds = new[] { targetNetState.NetworkId };
                PopulateSkillRequest(hit.point, ActionType.GeneralRevive, ref resultData);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Populates the ActionRequestData with additional information. The TargetIds of the action should already be set before calling this. 
        /// </summary>
        /// <param name="hitPoint">The point in world space where the click ray hit the target.</param>
        /// <param name="action">The action to perform (will be stamped on the resultData)</param>
        /// <param name="resultData">The ActionRequestData to be filled out with additional information.</param>
        private void PopulateSkillRequest(Vector3 hitPoint, ActionType action, ref ActionRequestData resultData)
        {
            resultData.ActionTypeEnum = action;
            var actionInfo = GameDataSource.Instance.ActionDataByType[action];

            //currently all skills but LaunchProjectile should trigger an implicit ChaseAction, so we default to true and disable in the subsequent switch-case.
            resultData.ShouldClose = true;

            switch (actionInfo.Logic)
            {
                //for projectile logic, infer the direction from the click position.
                case ActionLogic.LaunchProjectile:
                    Vector3 offset = hitPoint - transform.position;
                    offset.y = 0;
                    resultData.Direction = offset.normalized;
                    resultData.ShouldClose = false; //why? Because you could be lining up a shot, hoping to hit other people between you and your target. Moving you would be quite invasive. 
                    return;
                case ActionLogic.RangedFXTargeted:
                    if (resultData.TargetIds == null) { resultData.Position = hitPoint; }
                    return;
            }
        }

        void Update()
        {
            //we do this in "Update" rather than "FixedUpdate" because discrete clicks can be missed in FixedUpdate.
            if (Input.GetMouseButtonDown(1))
            {
                m_ClickRequest = Input.mousePosition;
            }

            m_EmoteAction = ActionType.None;
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                m_EmoteAction = ActionType.Emote1;
            }
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                m_EmoteAction = ActionType.Emote2;
            }
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                m_EmoteAction = ActionType.Emote3;
            }
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                m_EmoteAction = ActionType.Emote4;
            }
            if (m_EmoteAction != ActionType.None)
            {
                var emoteData = new ActionRequestData();
                emoteData.ActionTypeEnum = m_EmoteAction;
                emoteData.CancelMovement = true;
                m_NetworkCharacter.ClientSendActionRequest(ref emoteData);
            }

            if (Input.GetKeyUp("1"))
            {
                m_Skill2Request = true;
            }
        }
    }
}
