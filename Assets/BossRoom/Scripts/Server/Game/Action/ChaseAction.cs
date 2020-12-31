using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

namespace BossRoom.Server
{
    public class ChaseAction : Action
    {
        private NetworkedObject m_Target;
        private ServerCharacterMovement m_Movement;

        private Vector3 m_CurrentTargetPos;

        public ChaseAction(ServerCharacter parent, ref ActionRequestData data, int level) : base(parent, ref data, level)
        {
        }


        /// <summary>
        /// Called when the Action starts actually playing (which may be after it is created, because of queueing). 
        /// </summary>
        /// <returns>false if the action decided it doesn't want to run after all, true otherwise. </returns>
        public override bool Start()
        {
            if(m_data.TargetIds == null || m_data.TargetIds.Length == 0 || !MLAPI.Spawning.SpawnManager.SpawnedObjects.ContainsKey(m_data.TargetIds[0])  )
            {
                Debug.Log("Failed to start ChaseAction. The target entity  wasn't submitted or doesn't exist anymore" );
                return false;
            }

            m_Target = MLAPI.Spawning.SpawnManager.SpawnedObjects[m_data.TargetIds[0]];

            m_Movement = m_parent.GetComponent<ServerCharacterMovement>();
            m_Movement.SetMovementTarget(m_Target.transform.position);
            m_CurrentTargetPos = m_Target.transform.position;

            return true;
        }

        /// <summary>
        /// Called each frame while the action is running. 
        /// </summary>
        /// <returns>true to keep running, false to stop. The Action will stop by default when its duration expires, if it has a duration set. </returns>
        public override bool Update()
        {
            float dist_to_target = (m_parent.transform.position - m_Target.transform.position).magnitude;
            if( m_data.Amount > dist_to_target )
            {
                //we made it! we're done. 
                Cancel();
                return false;
            }

            float target_moved = (m_Target.transform.position - m_CurrentTargetPos).magnitude;
            if( m_data.Amount < target_moved )
            {
                //target has moved past our range tolerance. Must repath. 
                this.m_Movement.SetMovementTarget(m_Target.transform.position);
                m_CurrentTargetPos = m_Target.transform.position;
            }

            return true;
        }

        public override void Cancel()
        {
            if( m_Movement != null )
            {
                m_Movement.CancelMove();
            }
        }

    }

}
