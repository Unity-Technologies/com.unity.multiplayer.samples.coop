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

        private const float k_MoveSendRateSeconds = 0.5f;

        private float m_LastSentMove;

        // Cache raycast hit array so that we can use non alloc raycasts
        private readonly RaycastHit[] k_CachedHit = new RaycastHit[4];

        // This is basically a constant but layer masks cannot be created in the constructor, that's why it's assigned int Awake.
        private LayerMask k_GroundLayerMask;
        private LayerMask k_ActionLayerMask;

        private NetworkCharacterState m_NetworkCharacter;

        private enum SkillTriggerStyle
        {
            None,        //no skill was triggered.
            MouseClick,  //skill was triggered via mouse-click implying you should do a raycast from the mouse position to find a target.
            Keyboard,    //skill was triggered via a Keyboard press, implying target should be taken from the active target.
            UI,          //skill was triggered from the UI, and similar to Keyboard, target should be inferred from the active target. 
        }

        /// <summary>
        /// We detect clicks in Update (because you can miss single discrete clicks in FixedUpdate). But we need to
        /// raycast in FixedUpdate, because raycasts done in Update won't work reliably.
        /// This nullable vector will be set to a screen coordinate when an attack click was made.
        /// </summary>
        SkillTriggerStyle m_Skill1Request;
        SkillTriggerStyle m_Skill2Request;
        bool m_TargetRequest = false;
        bool m_SkillActive = false;
        bool m_MoveRequest = false;

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

            if (m_Skill2Request != SkillTriggerStyle.None)
            {
                var actionInput = GameDataSource.Instance.ActionDataByType[CharacterData.Skill2].ActionInput;
                if (actionInput != null)
                {
                    var skill2 = Instantiate(GameDataSource.Instance.ActionDataByType[CharacterData.Skill2].ActionInput);
                    skill2.Initiate(m_NetworkCharacter, CharacterData.Skill2, FinishSkill);
                    m_SkillActive = true;
                }
                else
                {
                    PerformSkill(CharacterData.Skill2, m_Skill2Request);
                }

                m_Skill2Request = SkillTriggerStyle.None;
                return;
            }

            if (m_TargetRequest == true)
            {
                PerformSkill(ActionType.GeneralTarget, SkillTriggerStyle.MouseClick);
                m_TargetRequest = false;
                return;
            }

            if (m_Skill1Request != SkillTriggerStyle.None)
            {
                PerformSkill(CharacterData.Skill1, m_Skill1Request);
                m_Skill1Request = SkillTriggerStyle.None;
                return;
            }

            if( m_MoveRequest )
            {
                m_MoveRequest = false;
                if ( (Time.time - m_LastSentMove) > k_MoveSendRateSeconds)
                {
                    m_LastSentMove = Time.time;
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
            }
        }

        /// <summary>
        /// Perform a skill in response to some input trigger. This is the common method to which all input-driven skill plays funnel. 
        /// </summary>
        /// <param name="actionType">The action you want to play. Note that "Skill1" may be overriden contextually depending on the target.</param>
        /// <param name="triggerStyle">What sort of input triggered this skill?</param>
        private void PerformSkill(ActionType actionType, SkillTriggerStyle triggerStyle)
        {
            int numHits = 0;
            if (triggerStyle == SkillTriggerStyle.MouseClick)
            {
                var ray = m_MainCamera.ScreenPointToRay(Input.mousePosition);
                numHits = Physics.RaycastNonAlloc(ray, k_CachedHit, k_MouseInputRaycastDistance, k_ActionLayerMask);
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

            Transform hitTransform = networkedHitIndex >= 0 ? k_CachedHit[networkedHitIndex].transform : null;
            if (GetActionRequestForTarget(hitTransform, actionType, triggerStyle, out ActionRequestData playerAction))
            {
                //Don't trigger our move logic for another 500ms. This protects us from moving  just because we clicked on them to target them.
                m_LastSentMove = Time.time;
                m_NetworkCharacter.ClientSendActionRequest(ref playerAction);
            }
            else
            {
                // clicked on nothing... perform a "miss" attack on the spot they clicked on
                var data = new ActionRequestData();
                PopulateSkillRequest(k_CachedHit[0].point, actionType, ref data);
                m_NetworkCharacter.ClientSendActionRequest(ref data);
            }
        }

        /// <summary>
        /// When you right-click on something you will want to do contextually different things. For example you might attack an enemy,
        /// but revive a friend. You might also decide to do nothing (e.g. right-clicking on a friend who hasn't FAINTED).
        /// </summary>
        /// <param name="hit">The Transform of the entity we clicked on, or null if none.</param>
        /// <param name="actionType">The Action to build for</param>
        /// <param name="triggerStyle">How did this skill play get triggered? Mouse, Keyboard, UI etc.</param>
        /// <param name="resultData">Out parameter that will be filled with the resulting action, if any.</param>
        /// <returns>true if we should play an action, false otherwise. </returns>
        private bool GetActionRequestForTarget(Transform hit, ActionType actionType, SkillTriggerStyle triggerStyle, out ActionRequestData resultData)
        {
            resultData = new ActionRequestData();

            var targetNetObj = hit != null ? hit.GetComponent<NetworkedObject>() : null;

            //if we can't get our target from the submitted hit transform, get it from our stateful target in our NetworkCharacterState. 
            if (!targetNetObj && actionType != ActionType.GeneralTarget)
            {
                ulong targetId = m_NetworkCharacter.TargetId.Value;
                if (ActionUtils.IsValidTarget(targetId))
                {
                    targetNetObj = MLAPI.Spawning.SpawnManager.SpawnedObjects[targetId];
                }
            }

            var targetNetState = targetNetObj != null ? targetNetObj.GetComponent<NetworkCharacterState>() : null;
            if (targetNetState == null)
            {
                //Not a Character. In the future this could represent interacting with some other interactable, but for
                //now, it implies we just do nothing.
                return false;
            }

            //Skill1 may be contextually overridden if it was generated from a mouse-click. 
            if (actionType == CharacterData.Skill1 && triggerStyle == SkillTriggerStyle.MouseClick)
            {
                if (!targetNetState.IsNpc && targetNetState.NetworkLifeState.Value == LifeState.Fainted)
                {
                    //right-clicked on a downed ally--change the skill play to Revive. 
                    actionType = ActionType.GeneralRevive;
                }
            }

            // record our target in case this action uses that info (non-targeted attacks will ignore this)
            resultData.ActionTypeEnum = actionType;
            resultData.TargetIds = new ulong[] { targetNetState.NetworkId };
            PopulateSkillRequest(targetNetState.transform.position, actionType, ref resultData);
            return true;
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

            //most skill types should implicitly close distance. The ones that don't are explicitly set to false in the following switch.
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
                case ActionLogic.Target:
                    resultData.ShouldClose = false;
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
                m_Skill1Request = SkillTriggerStyle.MouseClick;
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
                m_Skill2Request = SkillTriggerStyle.Keyboard;
            }

            if (Input.GetMouseButtonDown(0))
            {
                m_TargetRequest = true;
            }
            else if(Input.GetMouseButton(0) )
            {
                m_MoveRequest = true;
            }
        }
    }
}
