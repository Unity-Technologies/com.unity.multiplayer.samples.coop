using MLAPI;
using System;
using UnityEngine;
using UnityEngine.AI;

namespace BossRoom.Server
{
    public enum MovementState
    {
        Idle = 0,
        PathFollowing = 1,
    }

    /// <summary>
    /// Component responsible for moving a character on the server side based on inputs.
    /// </summary>
    [RequireComponent(typeof(NetworkCharacterState), typeof(NavMeshAgent), typeof(ServerCharacter)), RequireComponent(typeof(Rigidbody))]
    public class ServerCharacterMovement : NetworkedBehaviour
    {
        private NavMeshAgent m_NavMeshAgent;
        private Rigidbody m_Rigidbody;
        private NetworkCharacterState m_NetworkCharacterState;

        private NavMeshPath m_DesiredMovementPath;
        private MovementState m_MovementState;


        [SerializeField]
        private float m_MovementSpeed; // TODO [GOMPS-86] this should be assigned based on character definition 

        private void Awake()
        {
            m_NavMeshAgent = GetComponent<NavMeshAgent>();
            m_NetworkCharacterState = GetComponent<NetworkCharacterState>();
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        public override void NetworkStart()
        {
            if (!IsServer)
            {
                // Disable server component on clients
                enabled = false;
                return;
            }

            // On the server enable navMeshAgent and initialize
            m_NavMeshAgent.enabled = true;
            m_DesiredMovementPath = new NavMeshPath();
        }

        /// <summary>
        /// Sets a movement target. We will path to this position, avoiding static obstacles. 
        /// </summary>
        /// <param name="position">Position in world space to path to. </param>
        public void SetMovementTarget(Vector3 position)
        {
            m_MovementState = MovementState.PathFollowing;

            // Recalculate navigation path only on target change.
            m_NavMeshAgent.CalculatePath(position, m_DesiredMovementPath);
        }

        /// <summary>
        /// Cancels any moves that are currently in progress. 
        /// </summary>
        public void CancelMove()
        {
            m_MovementState = MovementState.Idle;
        }

        private void FixedUpdate()
        {
            if (m_MovementState == MovementState.PathFollowing)
            {
                Movement();
            }

            // Send new position values to the client
            m_NetworkCharacterState.NetworkPosition.Value = transform.position;
            m_NetworkCharacterState.NetworkRotationY.Value = transform.rotation.eulerAngles.y;
            m_NetworkCharacterState.NetworkMovementSpeed.Value = m_MovementState == MovementState.Idle ? 0 : m_MovementSpeed;
        }

        private void Movement()
        {
            var corners = m_DesiredMovementPath.corners;

            // If we don't have a movement path stop moving
            if (corners.Length == 0)
            {
                m_MovementState = MovementState.Idle;
                return;
            }

            var desiredMovementAmount = m_MovementSpeed * Time.fixedDeltaTime;

            // If there is less distance to move left in the path than our desired amount
            if (Vector3.SqrMagnitude(corners[corners.Length - 1] - transform.position) < (desiredMovementAmount * desiredMovementAmount))
            {
                // Set to destination and stop moving
                transform.position = corners[corners.Length - 1];
                m_MovementState = MovementState.Idle;
                return;
            }

            // Get the direction to move along based on the calculated path.
            var direction = corners.Length > 1
                ? (corners[1] - corners[0]).normalized
                : throw new InvalidOperationException("Navigation path should have a start and end position");


            var movementVector = direction * desiredMovementAmount;

            m_NavMeshAgent.Move(movementVector);
            transform.rotation = Quaternion.LookRotation(movementVector);

            //fixme--is this right? If I don't do this the Rigidbody is "left behind", and doesn't move with the GameObject. 
            //also see ClientCharacterMovement before deleting this comment. 
            m_Rigidbody.position = transform.position;
            m_Rigidbody.rotation = transform.rotation;

            m_NavMeshAgent.CalculatePath(corners[corners.Length - 1], m_DesiredMovementPath);
        }
    }
}
