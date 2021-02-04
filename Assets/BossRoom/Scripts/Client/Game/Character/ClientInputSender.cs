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
        private readonly RaycastHit[] k_CachedHit = new RaycastHit[1];

        // This is basically a constant but layer masks cannot be created in the constructor, that's why it's assigned int Awake.
        private LayerMask k_GroundLayerMask;
        private LayerMask k_ActionLayerMask;

        private NetworkCharacterState m_NetworkCharacter;

        /// <summary>
        /// We detect clicks in Update (because you can miss single discrete clicks in FixedUpdate). But we need to
        /// raycast in FixedUpdate, because raycasts done in Update won't work reliably.
        /// This nullable vector will be set to a screen coordinate when an attack click was made.
        /// </summary>
        private System.Nullable<Vector3> m_ClickRequest;

        ActionType m_EmoteAction;

        /// <summary>
        /// Convenience getter that returns our CharacterData
        /// </summary>
        private CharacterClass CharacterData
        {
            get { return GameDataSource.Instance.CharacterDataByType[m_NetworkCharacter.CharacterType.Value]; }
        }

        public override void NetworkStart()
        {
            // TODO Don't use NetworkedBehaviour for just NetworkStart [GOMPS-81]
            if (!IsClient || !IsOwner)
            {
                enabled = false;
            }

            k_GroundLayerMask = LayerMask.GetMask(new [] { "Ground" });
            k_ActionLayerMask = LayerMask.GetMask(new [] { "PCs", "NPCs", "Ground" });
    }

        public event Action<Vector3> OnClientClick;

        void Awake()
        {
            m_NetworkCharacter = GetComponent<NetworkCharacterState>();
        }

        void FixedUpdate()
        {
            // TODO replace with new Unity Input System [GOMPS-81]

            // Is mouse button pressed (not just checking for down to allow continuous movement inputs by holding the mouse button down)
            if (Input.GetMouseButton(0))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
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
                var ray = Camera.main.ScreenPointToRay(m_ClickRequest.Value);
                var rayCastHit = Physics.RaycastNonAlloc(ray, k_CachedHit, k_MouseInputRaycastDistance, k_ActionLayerMask) > 0;
                if (rayCastHit && GetTargetObject(ref k_CachedHit[0]) != 0)
                {
                    //if we have clicked on an enemy:
                    // - two actions will queue one after the other, causing us to run over to our target and take a swing.
                    //if we have clicked on a fallen friend - we will revive him

                    ActionRequestData playerAction;
                    bool doAction = GetActionRequestForTarget(ref k_CachedHit[0], out playerAction);

                    if (doAction)
                    {
                        float range = GameDataSource.Instance.ActionDataByType[playerAction.ActionTypeEnum].Range;
                        var chaseData = new ActionRequestData();
                        chaseData.ActionTypeEnum = ActionType.GeneralChase;
                        chaseData.Amount = range;
                        chaseData.TargetIds = new ulong[] { GetTargetObject(ref k_CachedHit[0]) };
                        m_NetworkCharacter.ClientSendActionRequest(ref chaseData);
                        m_NetworkCharacter.ClientSendActionRequest(ref playerAction);
                    }
                }
                else
                {
                    var data = new ActionRequestData();
                    PopulateSkillRequest(ref k_CachedHit[0], CharacterData.Skill1, ref data);
                    data.ActionTypeEnum = CharacterData.Skill1;
                    m_NetworkCharacter.ClientSendActionRequest(ref data);
                }

                m_ClickRequest = null;
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
                resultData.ShouldQueue = true; //wait your turn--don't clobber the chase action.
                PopulateSkillRequest(ref hit, CharacterData.Skill1, ref resultData);
                return true;
            }
            else if (targetNetState.NetworkLifeState.Value == LifeState.Fainted)
            {
                resultData = new ActionRequestData();
                resultData.ShouldQueue = true;
                resultData.ActionTypeEnum = ActionType.GeneralRevive;
                resultData.TargetIds = new[] { targetNetState.NetworkId };
                return true;
            }

            return false;
        }

        private void PopulateSkillRequest(ref RaycastHit hit, ActionType action, ref ActionRequestData resultData)
        {
            resultData.ActionTypeEnum = action;
            var actionInfo = GameDataSource.Instance.ActionDataByType[action];
            switch (actionInfo.Logic)
            {
                //for projectile logic, infer the direction from the click position. 
                case ActionLogic.LaunchProjectile:
                    Vector3 offset = hit.point - transform.position;
                    offset.y = 0;
                    resultData.Direction = offset.normalized;
                    return;
            }
        }

        private void Update()
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
        }

        /// <summary>
        /// Gets the Target NetworkId from the Raycast hit, or 0 if Raycast didn't contact a Networked Object.
        /// </summary>
        private ulong GetTargetObject(ref RaycastHit hit)
        {
            if (hit.collider == null)
            {
                return 0;
            }

            var targetObj = hit.collider.GetComponent<NetworkedObject>();
            if (targetObj == null) { return 0; }

            return targetObj.NetworkId;
        }
    }
}
