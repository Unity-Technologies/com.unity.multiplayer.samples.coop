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
            if( Data.TargetIds == null || Data.TargetIds.Length == 0 ) { return false; }

            if( !IsValidTarget(TargetId)) { return false; }

            m_Parent.NetState.TargetId.Value = TargetId;

            FaceTarget(TargetId);

            return true;
        }

        public override bool Update()
        {
            //TODO: it would be neat to actively turn to face our target, as long as we weren't doing anything else important.
            //The tricky bit would be the logic to make sure we weren't disrupting other Actions that also wanted to change our rotation.

            return IsValidTarget(TargetId);
        }

        public override void Cancel()
        {
            if( m_Parent.NetState.TargetId.Value == TargetId )
            {
                m_Parent.NetState.TargetId.Value = 0;
            }
        }

        private ulong TargetId { get { return Data.TargetIds[0]; } }

        private bool IsValidTarget(ulong targetId)
        {
            //note that we DON'T check if you're an ally. It's perfectly valid to target friends,
            //because there are friendly skills, such as Heal.

            if(!MLAPI.Spawning.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkedObject targetChar))
            {
                return false;
            }

            var targetNetState = targetChar.GetComponent<NetworkCharacterState>();
            if(targetNetState == null )
            {
                return false;
            }

            //only Dead characters are untargetable. All others are 
            return targetNetState.NetworkLifeState.Value != LifeState.Dead;
        }

        /// <summary>
        /// Only call this after validating the target via IsValidTarget.
        /// </summary>
        /// <param name="targetId"></param>
        private void FaceTarget(ulong targetId)
        {
            var targetPos = MLAPI.Spawning.SpawnManager.SpawnedObjects[targetId].transform.position;

            Vector3 diff = targetPos - m_Parent.transform.position;

            diff.y = 0;
            m_Parent.transform.forward = diff;
        }
    }
}

