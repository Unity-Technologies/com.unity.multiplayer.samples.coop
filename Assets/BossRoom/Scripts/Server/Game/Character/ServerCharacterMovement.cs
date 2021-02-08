using MLAPI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;

namespace BossRoom.Server
{
    public enum MovementState
    {
        Idle = 0,
        PathFollowing = 1,
        Charging = 2,
        Knockback = 3,
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
        private NavigationSystem m_NavigationSystem;

        private DynamicNavPath m_NavPath;

        private MovementState m_MovementState;
        private ServerCharacter m_CharLogic;

        [SerializeField]
        private float m_MovementSpeed; // TODO [GOMPS-86] this should be assigned based on character definition

        // when we are in charging and knockback mode, we use these additional variables
        private float m_ForcedSpeed;
        private float m_SpecialModeDurationRemaining;

        // this one is specific to knockback mode
        private Vector3 m_KnockbackVector;

        private void Awake()
        {
            m_NavMeshAgent = GetComponent<NavMeshAgent>();
            m_NetworkCharacterState = GetComponent<NetworkCharacterState>();
            m_CharLogic = GetComponent<ServerCharacter>();
            m_Rigidbody = GetComponent<Rigidbody>();

            m_NavigationSystem = GameObject.FindGameObjectWithTag(NavigationSystem.NavigationSystemTag).GetComponent<NavigationSystem>();
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
            m_NetworkCharacterState.OnReceivedClientInput += OnReceivedClientInput;
            m_NavPath = new DynamicNavPath(m_NavMeshAgent, m_NavigationSystem);
        }

        private void OnReceivedClientInput(Vector3 position)
        {
            m_CharLogic.ClearActions(); //a fresh movement request trumps whatever we were doing before. 
            SetMovementTarget(position);
        }

        /// <summary>
        /// Sets a movement target. We will path to this position, avoiding static obstacles. 
        /// </summary>
        /// <param name="position">Position in world space to path to. </param>
        public void SetMovementTarget(Vector3 position)
        {
            m_MovementState = MovementState.PathFollowing;
            m_NavPath.SetTargetPosition(position);
        }

        public void StartForwardCharge(float speed, float duration)
        {
            m_NavPath.Clear();
            m_MovementState = MovementState.Charging;
            m_ForcedSpeed = speed;
            m_SpecialModeDurationRemaining = duration;
        }

        public void StartKnockback(Vector3 knocker, float speed, float duration)
        {
            m_NavPath.Clear();
            m_MovementState = MovementState.Knockback;
            m_KnockbackVector = transform.position - knocker;
            m_ForcedSpeed = speed;
            m_SpecialModeDurationRemaining = duration;
        }

        /// <summary>
        /// Follow the given transform until it is reached.
        /// </summary>
        /// <param name="followTransform">The transform to follow</param>
        public void FollowTransform(Transform followTransform)
        {
            m_MovementState = MovementState.PathFollowing;
            m_NavPath.FollowTransform(followTransform);
        }

        /// <summary>
        /// Returns true if the current movement-mode is unabortable (e.g. a knockback effect)
        /// </summary>
        /// <returns></returns>
        public bool IsPerformingForcedMovement()
        {
            return m_MovementState != MovementState.Knockback;
        }

        /// <summary>
        /// Cancels any moves that are currently in progress. 
        /// </summary>
        public void CancelMove()
        {
            m_NavPath.Clear();
            m_MovementState = MovementState.Idle;
        }

        private void FixedUpdate()
        {
            PerformMovement();

            // Send new position values to the client
            m_NetworkCharacterState.NetworkPosition.Value = transform.position;
            m_NetworkCharacterState.NetworkRotationY.Value = transform.rotation.eulerAngles.y;
            m_NetworkCharacterState.NetworkMovementSpeed.Value = GetVisualMovementSpeed();
        }

        private void OnValidate()
        {
            if (gameObject.scene.rootCount > 1) // Hacky way for checking if this is a scene object or a prefab instance and not a prefab definition.
            {
                Assert.IsNotNull(
                    GameObject.FindGameObjectWithTag(NavigationSystem.NavigationSystemTag)?.GetComponent<NavigationSystem>(),
                    $"NavigationSystem not found. Is there a NavigationSystem Behaviour in the Scene and does its GameObject have the {NavigationSystem.NavigationSystemTag} tag? {gameObject.scene.name}"
                );
            }
        }

        private void OnDestroy()
        {
            m_NavPath.Dispose();
        }

        private void PerformMovement()
        {
            if (m_MovementState == MovementState.Idle)
                return;

            Vector3 movementVector;

            if (m_MovementState == MovementState.Charging)
            {
                // if we're done charging, stop moving
                m_SpecialModeDurationRemaining -= Time.fixedDeltaTime;
                if (m_SpecialModeDurationRemaining <= 0)
                {
                    m_MovementState = MovementState.Idle;
                    return;
                }

                var desiredMovementAmount = m_ForcedSpeed * Time.fixedDeltaTime;
                movementVector = transform.forward * desiredMovementAmount;
            }
            else if (m_MovementState == MovementState.Knockback)
            {
                m_SpecialModeDurationRemaining -= Time.fixedDeltaTime;
                if (m_SpecialModeDurationRemaining <= 0)
                {
                    m_MovementState = MovementState.Idle;
                    return;
                }

                var desiredMovementAmount = m_ForcedSpeed * Time.fixedDeltaTime;
                movementVector = m_KnockbackVector * desiredMovementAmount;
            }
            else
            {
                var desiredMovementAmount = m_MovementSpeed * Time.fixedDeltaTime;
                movementVector = m_NavPath.MoveAlongPath(desiredMovementAmount);

                // If we didn't move stop moving.
                if (movementVector == Vector3.zero)
                {
                    m_MovementState = MovementState.Idle;
                    return;
                }
            }

            m_NavMeshAgent.Move(movementVector);
            transform.rotation = Quaternion.LookRotation(movementVector);

            // After moving adjust the position of the dynamic rigidbody.
            m_Rigidbody.position = transform.position;
            m_Rigidbody.rotation = transform.rotation;
        }

        private float GetVisualMovementSpeed()
        {
            if (m_MovementState == MovementState.Idle || m_MovementState == MovementState.Knockback)
                return 0;
            else
                return m_MovementSpeed;
        }
    }
}
