using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    [CreateAssetMenu(menuName = "BossRoom/Actions/Chase Action")]
    public class ChaseAction : Action
    {
        private NetworkObject m_Target;

        Transform m_TargetTransform;

        /// <summary>
        /// Called when the Action starts actually playing (which may be after it is created, because of queueing).
        /// </summary>
        /// <returns>false if the action decided it doesn't want to run after all, true otherwise. </returns>
        public override bool OnStart(ServerCharacter serverCharacter)
        {
            if (!HasValidTarget())
            {
                Debug.Log("Failed to start ChaseAction. The target entity  wasn't submitted or doesn't exist anymore");
                return ActionConclusion.Stop;
            }

            m_Target = NetworkManager.Singleton.SpawnManager.SpawnedObjects[m_Data.TargetIds[0]];

            if (PhysicsWrapper.TryGetPhysicsWrapper(m_Data.TargetIds[0], out var physicsWrapper))
            {
                m_TargetTransform = physicsWrapper.Transform;
            }
            else
            {
                m_TargetTransform = m_Target.transform;
            }

            Vector3 currentTargetPos = m_TargetTransform.position;

            if (StopIfDone(serverCharacter))
            {
                serverCharacter.physicsWrapper.Transform.LookAt(currentTargetPos); //even if we didn't move, snap to face the target!
                return ActionConclusion.Stop;
            }

            if (!serverCharacter.Movement.IsPerformingForcedMovement())
            {
                serverCharacter.Movement.FollowTransform(m_TargetTransform);
            }
            return ActionConclusion.Continue;
        }

        public override void Reset()
        {
            base.Reset();
            m_Target = null;
            m_TargetTransform = null;
        }

        /// <summary>
        /// Returns true if our ActionRequestData came with a valid target. For the ChaseAction, this is pretty liberal (could be friend or foe, could be
        /// dead or alive--just needs to be present).
        /// </summary>
        private bool HasValidTarget()
        {
            return m_Data.TargetIds != null &&
                   m_Data.TargetIds.Length > 0 &&
                   NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(m_Data.TargetIds[0]);
        }

        /// <summary>
        /// Tests to see if we've reached our target. Returns true if we've reached our target, false otherwise (in which case it also stops our movement).
        /// </summary>
        private bool StopIfDone(ServerCharacter parent)
        {
            if (m_TargetTransform == null)
            {
                //if the target disappeared on us, then just stop.
                Cancel(parent);
                return true;
            }

            float distToTarget2 = (parent.physicsWrapper.Transform.position - m_TargetTransform.position).sqrMagnitude;
            if ((m_Data.Amount * m_Data.Amount) > distToTarget2)
            {
                //we made it! we're done.
                Cancel(parent);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Called each frame while the action is running.
        /// </summary>
        /// <returns>true to keep running, false to stop. The Action will stop by default when its duration expires, if it has a duration set. </returns>
        public override bool OnUpdate(ServerCharacter clientCharacter)
        {
            if (StopIfDone(clientCharacter)) { return ActionConclusion.Stop; }

            // Keep re-assigning our chase target whenever possible.
            // This way, if we get Knocked Back mid-chase, we pick right back up and continue the chase.
            if (!clientCharacter.Movement.IsPerformingForcedMovement())
            {
                clientCharacter.Movement.FollowTransform(m_TargetTransform);
            }

            return ActionConclusion.Continue;
        }

        public override void Cancel(ServerCharacter serverCharacter)
        {
            if (serverCharacter.Movement && !serverCharacter.Movement.IsPerformingForcedMovement())
            {
                serverCharacter.Movement.CancelMove();
            }
        }

        public override bool OnUpdateClient(ClientCharacter clientCharacter)
        {
            return ActionConclusion.Continue;
        }
    }
}
