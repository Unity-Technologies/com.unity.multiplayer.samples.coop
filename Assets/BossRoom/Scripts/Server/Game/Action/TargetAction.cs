using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

namespace BossRoom.Server
{
    /// <summary>
    /// The "Target" Action is not a skill, but rather the result of a user left-clicking an enemy. This
    /// Action runs persistently, and automatically resets the NetworkCharacterState.Target property if the
    /// target becomes ineligible (dies or disappears). Note that while Actions in general can have multiple targets,
    /// you as a player can only have a single target selected at a time (the character that your target reticule appears under). 
    /// </summary>
    public class TargetAction : Action
    {
        public TargetAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data) { }

        public override bool Start()
        {
            //we must always clear the existing target, even if we don't run. This is how targets get cleared--running a TargetAction
            //with no target selected.
            m_Parent.NetState.TargetId.Value = 0;

            //there can only be one TargetAction at a time!
            m_Parent.RunningActions.CancelRunningActionsByLogic(ActionLogic.Target, true, this);

            if ( Data.TargetIds == null || Data.TargetIds.Length == 0 ) { return false; }

            m_Parent.NetState.TargetId.Value = TargetId;

            FaceTarget(TargetId);

            return true;
        }

        public override bool Update()
        {
            bool isValid = ActionUtils.IsValidTarget(TargetId);

            if( m_Parent.RunningActions.RunningActionCount == 1 && !m_Parent.GetComponent<ServerCharacterMovement>().IsMoving() && isValid )
            {
                //we're the only action running, and we're not moving, so let's swivel to face our target, just to be cool!
                FaceTarget(TargetId);
            }

            return isValid;
        }

        public override void Cancel()
        {
            if( m_Parent.NetState.TargetId.Value == TargetId )
            {
                m_Parent.NetState.TargetId.Value = 0;
            }
        }

        private ulong TargetId { get { return Data.TargetIds[0]; } }

        /// <summary>
        /// Only call this after validating the target via IsValidTarget.
        /// </summary>
        /// <param name="targetId"></param>
        private void FaceTarget(ulong targetId)
        {
            if( MLAPI.Spawning.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkedObject targetObject))
            {
                Vector3 diff = targetObject.transform.position - m_Parent.transform.position;

                diff.y = 0;
                if(diff != Vector3.zero )
                {
                    m_Parent.transform.forward = diff;
                }
            }
        }
    }
}

