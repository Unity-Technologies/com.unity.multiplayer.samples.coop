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

        private ServerCharacterMovement m_Movement;

        public override bool OnStart(ServerCharacter parent)
        {
            //we must always clear the existing target, even if we don't run. This is how targets get cleared--running a TargetAction
            //with no target selected.
            parent.NetState.TargetId.Value = 0;

            //there can only be one TargetAction at a time!
            parent.ActionPlayer.CancelRunningActionsByLogic(ActionLogic.Target, true, this);

            if (Data.TargetIds == null || Data.TargetIds.Length == 0) { return false; }

            m_Movement = parent.Movement;

            parent.NetState.TargetId.Value = TargetId;

            FaceTarget(TargetId);

            return true;
        }

        public override bool OnUpdate()
        {
            bool isValid = ActionUtils.IsValidTarget(TargetId);

            if (m_ServerParent.RunningActions.RunningActionCount == 1 && !m_Movement.IsMoving() && isValid)
            {
                //we're the only action running, and we're not moving, so let's swivel to face our target, just to be cool!
                FaceTarget(TargetId);
            }

            return isValid;
        }

        public override void Cancel()
        {
            if (m_ServerParent.NetState.TargetId.Value == TargetId)
            {
                m_ServerParent.NetState.TargetId.Value = 0;
            }
        }

        private ulong TargetId { get { return Data.TargetIds[0]; } }

        /// <summary>
        /// Only call this after validating the target via IsValidTarget.
        /// </summary>
        /// <param name="targetId"></param>
        private void FaceTarget(ulong targetId)
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

                Vector3 diff = targetObjectPosition - m_ServerParent.physicsWrapper.Transform.position;

                diff.y = 0;
                if (diff != Vector3.zero)
                {
                    m_ServerParent.physicsWrapper.Transform.forward = diff;
                }
            }
        }
    }
}

