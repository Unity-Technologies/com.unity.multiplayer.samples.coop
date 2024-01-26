using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    /// <summary>
    /// The "Target" Action is not a skill, but rather the result of a user left-clicking an enemy. This
    /// Action runs persistently, and automatically resets the NetworkCharacterState.Target property if the
    /// target becomes ineligible (dies or disappears). Note that while Actions in general can have multiple targets,
    /// you as a player can only have a single target selected at a time (the character that your target reticule appears under).
    /// </summary>
    [CreateAssetMenu(menuName = "BossRoom/Actions/Target Action")]
    public partial class TargetAction : Action
    {
        public override bool OnStart(ServerCharacter serverCharacter)
        {
            //we must always clear the existing target, even if we don't run. This is how targets get cleared--running a TargetAction
            //with no target selected.
            serverCharacter.TargetId.Value = 0;

            //there can only be one TargetAction at a time!
            serverCharacter.ActionPlayer.CancelRunningActionsByLogic(ActionLogic.Target, true, this);

            if (Data.TargetIds == null || Data.TargetIds.Length == 0) { return false; }

            serverCharacter.TargetId.Value = TargetId;

            FaceTarget(serverCharacter, TargetId);

            return true;
        }

        public override void Reset()
        {
            base.Reset();
            m_TargetReticule = null;
            m_CurrentTarget = 0;
            m_NewTarget = 0;
        }

        public override bool OnUpdate(ServerCharacter clientCharacter)
        {
            bool isValid = ActionUtils.IsValidTarget(TargetId);

            if (clientCharacter.ActionPlayer.RunningActionCount == 1 && !clientCharacter.Movement.IsMoving() && isValid)
            {
                //we're the only action running, and we're not moving, so let's swivel to face our target, just to be cool!
                FaceTarget(clientCharacter, TargetId);
            }

            return isValid;
        }

        public override void Cancel(ServerCharacter serverCharacter)
        {
            if (serverCharacter.TargetId.Value == TargetId)
            {
                serverCharacter.TargetId.Value = 0;
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
    }
}

