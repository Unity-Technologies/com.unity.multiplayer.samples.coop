using Unity.Multiplayer.Samples.BossRoom.Server;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Actions
{
    /// <summary>
    /// The "Target" Action is not a skill, but rather the result of a user left-clicking an enemy. This
    /// Action runs persistently, and automatically resets the NetworkCharacterState.Target property if the
    /// target becomes ineligible (dies or disappears). Note that while Actions in general can have multiple targets,
    /// you as a player can only have a single target selected at a time (the character that your target reticule appears under).
    /// </summary>
    public class TargetAction : Action
    {
        public TargetAction(ref ActionRequestData data) : base(ref data) { }

        private GameObject m_TargetReticule;
        private ulong m_CurrentTarget;
        private ulong m_NewTarget;
        private NetworkCharacterState m_ParentState;

        private const float k_ReticuleGroundHeight = 0.2f;

        public override bool OnStart(ServerCharacter parent)
        {
            //we must always clear the existing target, even if we don't run. This is how targets get cleared--running a TargetAction
            //with no target selected.
            parent.NetState.TargetId.Value = 0;

            //there can only be one TargetAction at a time!
            parent.ActionPlayer.CancelRunningActionsByLogic(ActionLogic.Target, true, this);

            if (Data.TargetIds == null || Data.TargetIds.Length == 0) { return false; }

            parent.NetState.TargetId.Value = TargetId;

            FaceTarget(parent, TargetId);

            return true;
        }

        public override bool OnUpdate(ServerCharacter parent)
        {
            bool isValid = ActionUtils.IsValidTarget(TargetId);

            if (parent.ActionPlayer.RunningActionCount == 1 && !parent.Movement.IsMoving() && isValid)
            {
                //we're the only action running, and we're not moving, so let's swivel to face our target, just to be cool!
                FaceTarget(parent,TargetId);
            }

            return isValid;
        }

        public override void Cancel(ServerCharacter parent)
        {
            if (parent.NetState.TargetId.Value == TargetId)
            {
                parent.NetState.TargetId.Value = 0;
            }
        }

        private ulong TargetId { get { return Data.TargetIds[0]; } }

        /// <summary>
        /// Only call this after validating the target via IsValidTarget.
        /// </summary>
        /// <param name="targetId"></param>
        private void FaceTarget(ServerCharacter parent, ulong targetId)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var targetObject))
            {
                Vector3 targetObjectPosition;

                if (targetObject.TryGetComponent(out ServerCharacter serverCharacter))
                {
                    targetObjectPosition = serverCharacter.physicsWrapper.Transform.position;
                }
                else
                {
                    targetObjectPosition = targetObject.transform.position;
                }

                Vector3 diff = targetObjectPosition - parent.physicsWrapper.Transform.position;

                diff.y = 0;
                if (diff != Vector3.zero)
                {
                    parent.physicsWrapper.Transform.forward = diff;
                }
            }
        }


        public override bool OnStartClient(ClientCharacterVisualization parent)
        {
            base.OnStartClient(parent);
            m_ParentState = parent.NetState;

            m_ParentState.TargetId.OnValueChanged += OnTargetChanged;
            m_ParentState.GetComponent<Client.ClientInputSender>().ActionInputEvent += OnActionInput;

            return true;
        }

        private void OnTargetChanged(ulong oldTarget, ulong newTarget)
        {
            m_NewTarget = newTarget;
        }

        private void OnActionInput(ActionRequestData data)
        {
            //this method runs on the owning client, and allows us to anticipate our new target for purposes of FX visualization.
            if (data.ActionTypeEnum == ActionType.GeneralTarget)
            {
                m_NewTarget = data.TargetIds[0];
            }
        }

        public override bool OnUpdateClient(ClientCharacterVisualization parent)
        {
            if (m_CurrentTarget != m_NewTarget)
            {
                m_CurrentTarget = m_NewTarget;

                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(m_CurrentTarget, out NetworkObject targetObject))
                {
                    var targetEntity = targetObject != null ? targetObject.GetComponent<ITargetable>() : null;
                    if (targetEntity != null)
                    {
                        ValidateReticule(parent, targetObject);
                        m_TargetReticule.SetActive(true);

                        var parentTransform = targetObject.transform;
                        if (targetObject.TryGetComponent(out Client.ClientCharacter clientCharacter) && clientCharacter.ChildVizObject)
                        {
                            //for characters, attach the reticule to the child graphics object.
                            parentTransform = clientCharacter.ChildVizObject.transform;
                        }

                        m_TargetReticule.transform.parent = parentTransform;
                        m_TargetReticule.transform.localPosition = new Vector3(0, k_ReticuleGroundHeight, 0);
                    }

                }
                else
                {
                    // null check here in case the target was destroyed along with the target reticule
                    if (m_TargetReticule != null)
                    {
                        m_TargetReticule.transform.parent = null;
                        m_TargetReticule.SetActive(false);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Ensures that the TargetReticule GameObject exists. This must be done prior to enabling it because it can be destroyed
        /// "accidentally" if its parent is destroyed while it is detached.
        /// </summary>
        private void ValidateReticule(ClientCharacterVisualization parent, NetworkObject targetObject)
        {
            if (m_TargetReticule == null)
            {
                m_TargetReticule = Object.Instantiate(parent.TargetReticulePrefab);
            }

            bool target_isnpc = targetObject.GetComponent<ITargetable>().IsNpc;
            bool myself_isnpc = m_ParentState.CharacterClass.IsNpc;
            bool hostile = target_isnpc != myself_isnpc;

            m_TargetReticule.GetComponent<MeshRenderer>().material = hostile ? parent.ReticuleHostileMat : parent.ReticuleFriendlyMat;
        }


        public override void CancelClient(ClientCharacterVisualization parent)
        {
            GameObject.Destroy(m_TargetReticule);

            m_ParentState.TargetId.OnValueChanged -= OnTargetChanged;
            if (m_ParentState.TryGetComponent(out Client.ClientInputSender inputSender))
            {
                inputSender.ActionInputEvent -= OnActionInput;
            }
        }
    }
}

