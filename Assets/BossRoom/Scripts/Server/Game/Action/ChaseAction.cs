using MLAPI;
using MLAPI.Spawning;
using UnityEngine;

namespace BossRoom.Server
{
    public class ChaseAction : Action
    {
        private NetworkObject m_Target;
        private ServerCharacterMovement m_Movement;

        public ChaseAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
        }


        /// <summary>
        /// Called when the Action starts actually playing (which may be after it is created, because of queueing).
        /// </summary>
        /// <returns>false if the action decided it doesn't want to run after all, true otherwise. </returns>
        public override bool Start()
        {
            if (!HasValidTarget())
            {
                Debug.Log("Failed to start ChaseAction. The target entity  wasn't submitted or doesn't exist anymore");
                return ActionConclusion.Stop;
            }

            m_Target = NetworkSpawnManager.SpawnedObjects[m_Data.TargetIds[0]];

            m_Movement = m_Parent.GetComponent<ServerCharacterMovement>();
            Vector3 currentTargetPos = m_Target.transform.position;

            if (StopIfDone())
            {
                m_Parent.transform.LookAt(currentTargetPos); //even if we didn't move, snap to face the target!
                return ActionConclusion.Stop;
            }

            if (!m_Movement.IsPerformingForcedMovement())
            {
                m_Movement.FollowTransform(m_Target.transform);
            }
            return ActionConclusion.Continue;
        }

        /// <summary>
        /// Returns true if our ActionRequestData came with a valid target. For the ChaseAction, this is pretty liberal (could be friend or foe, could be
        /// dead or alive--just needs to be present).
        /// </summary>
        private bool HasValidTarget()
        {
            return m_Data.TargetIds != null &&
                   m_Data.TargetIds.Length > 0 &&
                   NetworkSpawnManager.SpawnedObjects.ContainsKey(m_Data.TargetIds[0]);
        }

        /// <summary>
        /// Tests to see if we've reached our target. Returns true if we've reached our target, false otherwise (in which case it also stops our movement).
        /// </summary>
        private bool StopIfDone()
        {
            if(m_Target == null )
            {
                //if the target disappeared on us, then just stop.
                Cancel();
                return true;
            }

            float distToTarget2 = (m_Parent.transform.position - m_Target.transform.position).sqrMagnitude;
            if ((m_Data.Amount * m_Data.Amount) > distToTarget2)
            {
                //we made it! we're done.
                Cancel();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Called each frame while the action is running.
        /// </summary>
        /// <returns>true to keep running, false to stop. The Action will stop by default when its duration expires, if it has a duration set. </returns>
        public override bool Update()
        {
            if (StopIfDone()) { return ActionConclusion.Stop; }

            // Keep re-assigning our chase target whenever possible.
            // This way, if we get Knocked Back mid-chase, we pick right back up and continue the chase.
            if (!m_Movement.IsPerformingForcedMovement())
            {
                m_Movement.FollowTransform(m_Target.transform);
            }

            return ActionConclusion.Continue;
        }

        public override void Cancel()
        {
            if (m_Movement && !m_Movement.IsPerformingForcedMovement())
            {
                m_Movement.CancelMove();
            }
        }
    }
}
