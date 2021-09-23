using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Captures inputs for a character on a client and sends them to the server.
    /// </summary>
    [RequireComponent(typeof(NetworkCharacterState))]
    public class ClientInputSender : NetworkBehaviour
    {
        private const float k_MouseInputRaycastDistance = 100f;

        //The movement input rate is capped at 50ms (or 20 fps). This provides a nice balance between responsiveness and
        //upstream network conservation. This matters when holding down your mouse button to move.
        private const float k_MoveSendRateSeconds = 0.05f; //20 fps.


        private const float k_TargetMoveTimeout = 0.45f;  //prevent moves for this long after targeting someone (helps prevent walking to the guy you clicked).

        private float m_LastSentMove;

        // Cache raycast hit array so that we can use non alloc raycasts
        private readonly RaycastHit[] k_CachedHit = new RaycastHit[4];

        // This is basically a constant but layer masks cannot be created in the constructor, that's why it's assigned int Awake.
        private LayerMask k_GroundLayerMask;
        private LayerMask k_ActionLayerMask;

        private NetworkCharacterState m_NetworkCharacter;

        /// <summary>
        /// This event fires at the time when an action request is sent to the server.
        /// </summary>
        public Action<ActionRequestData> ActionInputEvent;

        /// <summary>
        /// This describes how a skill was requested. Skills requested via mouse click will do raycasts to determine their target; skills requested
        /// in other matters will use the stateful target stored in NetworkCharacterState.
        /// </summary>
        public enum SkillTriggerStyle
        {
            None,        //no skill was triggered.
            MouseClick,  //skill was triggered via mouse-click implying you should do a raycast from the mouse position to find a target.
            Keyboard,    //skill was triggered via a Keyboard press, implying target should be taken from the active target.
            KeyboardRelease, //represents a released key.
            UI,          //skill was triggered from the UI, and similar to Keyboard, target should be inferred from the active target.
            UIRelease,   //represents letting go of the mouse-button on a UI button
        }

        private bool IsReleaseStyle(SkillTriggerStyle style)
        {
            return style == SkillTriggerStyle.KeyboardRelease || style == SkillTriggerStyle.UIRelease;
        }

        /// <summary>
        /// This struct essentially relays the call params of RequestAction to FixedUpdate. Recall that we may need to do raycasts
        /// as part of doing the action, and raycasts done outside of FixedUpdate can give inconsistent results (otherwise we would
        /// just expose PerformAction as a public method, and let it be called in whatever scoped it liked.
        /// </summary>
        /// <remarks>
        /// Reference: https://answers.unity.com/questions/1141633/why-does-fixedupdate-work-when-update-doesnt.html
        /// </remarks>
        private struct ActionRequest
        {
            public SkillTriggerStyle TriggerStyle;
            public ActionType RequestedAction;
            public ulong TargetId;
        }

        /// <summary>
        /// List of ActionRequests that have been received since the last FixedUpdate ran. This is a static array, to avoid allocs, and
        /// because we don't really want to let this list grow indefinitely.
        /// </summary>
        private readonly ActionRequest[] m_ActionRequests = new ActionRequest[5];

        /// <summary>
        /// Number of ActionRequests that have been queued since the last FixedUpdate.
        /// </summary>
        private int m_ActionRequestCount;

        private BaseActionInput m_CurrentSkillInput = null;
        private bool m_MoveRequest = false;


        Camera m_MainCamera;

        public event Action<Vector3> ClientMoveEvent;

        [SerializeField]
        CharacterClassContainer m_CharacterClassContainer;

        /// <summary>
        /// Convenience getter that returns our CharacterData
        /// </summary>
        CharacterClass CharacterData => m_CharacterClassContainer.CharacterClass;

        [SerializeField]
        PhysicsWrapper m_PhysicsWrapper;

        public override void OnNetworkSpawn()
        {
            if (!IsClient || !IsOwner)
            {
                enabled = false;
                // dont need to do anything else if not the owner
                return;
            }

            k_GroundLayerMask = LayerMask.GetMask(new[] { "Ground" });
            k_ActionLayerMask = LayerMask.GetMask(new[] { "PCs", "NPCs", "Ground" });
        }

        void Awake()
        {
            m_NetworkCharacter = GetComponent<NetworkCharacterState>();
            m_MainCamera = Camera.main;
        }

        void FinishSkill()
        {
            m_CurrentSkillInput = null;
        }

        void SendInput(ActionRequestData action)
        {
            ActionInputEvent?.Invoke(action);
            m_NetworkCharacter.RecvDoActionServerRPC(action);
        }

        void FixedUpdate()
        {
            //play all ActionRequests, in FIFO order.
            for (int i = 0; i < m_ActionRequestCount; ++i)
            {
                if (m_CurrentSkillInput != null)
                {
                    //actions requested while input is active are discarded, except for "Release" requests, which go through.
                    if (IsReleaseStyle(m_ActionRequests[i].TriggerStyle))
                    {
                        m_CurrentSkillInput.OnReleaseKey();
                    }
                }
                else if (!IsReleaseStyle(m_ActionRequests[i].TriggerStyle))
                {
                    var actionData = GameDataSource.Instance.ActionDataByType[m_ActionRequests[i].RequestedAction];
                    if (actionData.ActionInput != null)
                    {
                        var skillPlayer = Instantiate(actionData.ActionInput);
                        skillPlayer.Initiate(m_NetworkCharacter, m_PhysicsWrapper.Transform.position, actionData.ActionTypeEnum, SendInput, FinishSkill);
                        m_CurrentSkillInput = skillPlayer;
                    }
                    else
                    {
                        PerformSkill(actionData.ActionTypeEnum, m_ActionRequests[i].TriggerStyle, m_ActionRequests[i].TargetId);
                    }
                }
            }

            m_ActionRequestCount = 0;

            if (m_MoveRequest)
            {
                m_MoveRequest = false;
                if ( (Time.time - m_LastSentMove) > k_MoveSendRateSeconds)
                {
                    m_LastSentMove = Time.time;
                    var ray = m_MainCamera.ScreenPointToRay(Input.mousePosition);
                    if (Physics.RaycastNonAlloc(ray, k_CachedHit, k_MouseInputRaycastDistance, k_GroundLayerMask) > 0)
                    {
                        m_NetworkCharacter.SendCharacterInputServerRpc(k_CachedHit[0].point);

                        //Send our client only click request
                        ClientMoveEvent?.Invoke(k_CachedHit[0].point);
                    }
                }
            }
        }

        /// <summary>
        /// Perform a skill in response to some input trigger. This is the common method to which all input-driven skill plays funnel.
        /// </summary>
        /// <param name="actionType">The action you want to play. Note that "Skill1" may be overriden contextually depending on the target.</param>
        /// <param name="triggerStyle">What sort of input triggered this skill?</param>
        /// <param name="targetId">(optional) Pass in a specific networkID to target for this action</param>
        private void PerformSkill(ActionType actionType, SkillTriggerStyle triggerStyle, ulong targetId = 0)
        {
            Transform hitTransform = null;

            if (targetId != 0)
            {
                // if a targetId is given, try to find the object
                NetworkObject targetNetObj;
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out targetNetObj))
                {
                    hitTransform = targetNetObj.transform;
                }
            }
            else
            {
                // otherwise try to find an object under the input position
                int numHits = 0;
                if (triggerStyle == SkillTriggerStyle.MouseClick)
                {
                    var ray = m_MainCamera.ScreenPointToRay(Input.mousePosition);
                    numHits = Physics.RaycastNonAlloc(ray, k_CachedHit, k_MouseInputRaycastDistance, k_ActionLayerMask);
                }

                int networkedHitIndex = -1;
                for (int i = 0; i < numHits; i++)
                {
                    if (k_CachedHit[i].transform.GetComponentInParent<NetworkObject>())
                    {
                        networkedHitIndex = i;
                        break;
                    }
                }

                hitTransform = networkedHitIndex >= 0 ? k_CachedHit[networkedHitIndex].transform : null;
            }

            if (GetActionRequestForTarget(hitTransform, actionType, triggerStyle, out ActionRequestData playerAction))
            {
                //Don't trigger our move logic for a while. This protects us from moving just because we clicked on them to target them.
                m_LastSentMove = Time.time + k_TargetMoveTimeout;

                SendInput(playerAction);
            }
            else if(actionType != ActionType.GeneralTarget )
            {
                // clicked on nothing... perform an "untargeted" attack on the spot they clicked on.
                // (Different Actions will deal with this differently. For some, like archer arrows, this will fire an arrow
                // in the desired direction. For others, like mage's bolts, this will fire a "miss" projectile at the spot clicked on.)

                var data = new ActionRequestData();
                PopulateSkillRequest(k_CachedHit[0].point, actionType, ref data);

                SendInput(data);
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

            var targetNetObj = hit != null ? hit.GetComponentInParent<NetworkObject>() : null;

            //if we can't get our target from the submitted hit transform, get it from our stateful target in our NetworkCharacterState.
            if (!targetNetObj && actionType != ActionType.GeneralTarget)
            {
                ulong targetId = m_NetworkCharacter.TargetId.Value;
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out targetNetObj);
            }

            //sanity check that this is indeed a valid target.
            if(targetNetObj==null || !ActionUtils.IsValidTarget(targetNetObj.NetworkObjectId))
            {
                return false;
            }

            var targetNetState = targetNetObj.GetComponent<NetworkCharacterState>();
            if (targetNetState != null)
            {
                //Skill1 may be contextually overridden if it was generated from a mouse-click.
                if (actionType == CharacterData.Skill1 && triggerStyle == SkillTriggerStyle.MouseClick)
                {
                    if (!targetNetState.IsNpc && targetNetState.LifeState == LifeState.Fainted)
                    {
                        //right-clicked on a downed ally--change the skill play to Revive.
                        actionType = ActionType.GeneralRevive;
                    }
                }
            }

            Vector3 targetHitPoint;
            if (PhysicsWrapper.TryGetPhysicsWrapper(targetNetObj.NetworkObjectId, out var movementContainer))
            {
                targetHitPoint = movementContainer.Transform.position;
            }
            else
            {
                targetHitPoint = targetNetObj.transform.position;
            }

            // record our target in case this action uses that info (non-targeted attacks will ignore this)
            resultData.ActionTypeEnum = actionType;
            resultData.TargetIds = new ulong[] { targetNetObj.NetworkObjectId };
            PopulateSkillRequest(targetHitPoint, actionType, ref resultData);
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

            // figure out the Direction in case we want to send it
            Vector3 offset = hitPoint - m_PhysicsWrapper.Transform.position;
            offset.y = 0;
            Vector3 direction = offset.normalized;

            switch (actionInfo.Logic)
            {
                //for projectile logic, infer the direction from the click position.
                case ActionLogic.LaunchProjectile:
                    resultData.Direction = direction;
                    resultData.ShouldClose = false; //why? Because you could be lining up a shot, hoping to hit other people between you and your target. Moving you would be quite invasive.
                    return;
                case ActionLogic.Melee:
                    resultData.Direction = direction;
                    return;
                case ActionLogic.Target:
                    resultData.ShouldClose = false;
                    return;
                case ActionLogic.Emote:
                    resultData.CancelMovement = true;
                    return;
                case ActionLogic.RangedFXTargeted:
                    resultData.Position = hitPoint;
                    return;
                case ActionLogic.DashAttack:
                    resultData.Position = hitPoint;
                    return;
            }
        }

        /// <summary>
        /// Request an action be performed. This will occur on the next FixedUpdate.
        /// </summary>
        /// <param name="action">the action you'd like to perform. </param>
        /// <param name="triggerStyle">What input style triggered this action.</param>
        public void RequestAction(ActionType action, SkillTriggerStyle triggerStyle, ulong targetId = 0)
        {
            // do not populate an action request unless said action is valid
            if (action == ActionType.None)
            {
                return;
            }

            Assert.IsTrue(GameDataSource.Instance.ActionDataByType.ContainsKey(action),
                $"Action {action} must be part of ActionData dictionary!");

            if (m_ActionRequestCount < m_ActionRequests.Length)
            {
                m_ActionRequests[m_ActionRequestCount].RequestedAction = action;
                m_ActionRequests[m_ActionRequestCount].TriggerStyle = triggerStyle;
                m_ActionRequests[m_ActionRequestCount].TargetId = targetId;
                m_ActionRequestCount++;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                RequestAction(CharacterData.Skill1, SkillTriggerStyle.Keyboard);
            }
            else if (Input.GetKeyUp(KeyCode.Alpha1))
            {
                RequestAction(CharacterData.Skill1, SkillTriggerStyle.KeyboardRelease);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                RequestAction(CharacterData.Skill2, SkillTriggerStyle.Keyboard);
            }
            else if (Input.GetKeyUp(KeyCode.Alpha2))
            {
                RequestAction(CharacterData.Skill2, SkillTriggerStyle.KeyboardRelease);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                RequestAction(CharacterData.Skill3, SkillTriggerStyle.Keyboard);
            }
            else if (Input.GetKeyUp(KeyCode.Alpha3))
            {
                RequestAction(CharacterData.Skill3, SkillTriggerStyle.KeyboardRelease);
            }

            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                RequestAction(ActionType.Emote1, SkillTriggerStyle.Keyboard);
            }
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                RequestAction(ActionType.Emote2, SkillTriggerStyle.Keyboard);
            }
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                RequestAction(ActionType.Emote3, SkillTriggerStyle.Keyboard);
            }
            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                RequestAction(ActionType.Emote4, SkillTriggerStyle.Keyboard);
            }

            if ( !EventSystem.current.IsPointerOverGameObject() && m_CurrentSkillInput == null)
            {
                //IsPointerOverGameObject() is a simple way to determine if the mouse is over a UI element. If it is, we don't perform mouse input logic,
                //to model the button "blocking" mouse clicks from falling through and interacting with the world.

                if (Input.GetMouseButtonDown(1))
                {
                    RequestAction(CharacterData.Skill1, SkillTriggerStyle.MouseClick);
                }

                if (Input.GetMouseButtonDown(0))
                {
                    RequestAction(ActionType.GeneralTarget, SkillTriggerStyle.MouseClick);
                }
                else if (Input.GetMouseButton(0))
                {
                    m_MoveRequest = true;
                }
            }
        }
    }
}
