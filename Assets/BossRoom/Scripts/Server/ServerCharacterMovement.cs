using System;
using System.Linq;
using BossRoom.Shared;
using MLAPI;
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
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NetworkCharacterState))]
    public class ServerCharacterMovement : NetworkedBehaviour
    {
        private NavMeshAgent navMeshAgent;
        private NetworkCharacterState networkCharacterState;

        private NavMeshPath path;
        private MovementState movementState;

        [SerializeField]
        private float movementSpeed; // TODO this should be assigned based on character definition

        public override void NetworkStart()
        {
            if (!IsServer)
            {
                // Disable server component on clients
                enabled = false;
                return;
            }

            // On the server enable navMeshAgent and initialize
            navMeshAgent.enabled = true;
            networkCharacterState.OnReceivedClientInput += SetMovementTarget;
            path = new NavMeshPath();
        }

        private void SetMovementTarget(Vector3 position)
        {
            movementState = MovementState.PathFollowing;

            // Recalculate navigation path only on target change.
            navMeshAgent.CalculatePath(position, path);

        }

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            networkCharacterState = GetComponent<NetworkCharacterState>();
        }

        private void FixedUpdate()
        {
            if (movementState == MovementState.PathFollowing)
            {
                Movement();
            }

            // Send new position values to the client
            networkCharacterState.NetworkPosition.Value = transform.position;
            networkCharacterState.NetworkRotationY.Value = transform.rotation.eulerAngles.y;
        }

        private void Movement()
        {
            var corners = path.corners; // TODO: maybe use non-alloc version

            // If we don't have a movement path stop moving
            if (!corners.Any())
            {
                movementState = MovementState.Idle;
                return;
            }

            var desiredMovementAmount = movementSpeed * Time.fixedDeltaTime;

            // If there is less distance to move left in the path than our desired amount
            if (Vector3.SqrMagnitude(corners[corners.Length - 1] - transform.position) < (desiredMovementAmount * desiredMovementAmount))
            {
                // Set to destination and stop moving
                transform.position = corners[corners.Length - 1];
                movementState = MovementState.Idle;
                return;
            }

            // Get the direction to move along based on the calculated path.
            var direction = corners.Length > 1
                ? (corners[1] - corners[0]).normalized
                : throw new InvalidOperationException("Navigation path should have a start and end position");


            var movementVector = direction * desiredMovementAmount;

            navMeshAgent.Move(movementVector);
            transform.rotation = Quaternion.LookRotation(movementVector);
            navMeshAgent.CalculatePath(corners[corners.Length - 1], path);
        }
    }
}