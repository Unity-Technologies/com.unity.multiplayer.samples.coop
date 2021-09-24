using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    public sealed class DynamicNavPath : IDisposable
    {
        /// <summary>
        /// The tolerance to decide whether the path needs to be recalculated when the position of a target transform changed.
        /// </summary>
        const float k_RepathToleranceSqr = 9f;

        NavMeshAgent m_Agent;

        NavigationSystem m_NavigationSystem;

        /// <summary>
        /// The target position value which was used to calculate the current path.
        /// This get stored to make sure the path gets recalculated if the target
        /// </summary>
        Vector3 m_CurrentPathOriginalTarget;

        /// <summary>
        /// This field caches a NavMesh Path so that we don't have to allocate a new one each time.
        /// </summary>
        NavMeshPath m_NavMeshPath;

        /// <summary>
        /// The remaining path points to follow to reach the target position.
        /// </summary>
        List<Vector3> m_Path;

        /// <summary>
        /// The target position of this path.
        /// </summary>
        Vector3 m_PositionTarget;

        /// <summary>
        /// A moving transform target, the path will readjust when the target moves. If this is non-null, it takes precedence over m_PositionTarget.
        /// </summary>
        Transform m_TransformTarget;

        /// <summary>
        /// Creates a new instance of the <see cref="DynamicNavPath"/>.
        /// </summary>
        /// <param name="agent">The NavMeshAgent of the object which uses this path.</param>
        /// <param name="navigationSystem">The navigation system which updates this path.</param>
        public DynamicNavPath(NavMeshAgent agent, NavigationSystem navigationSystem)
        {
            m_Agent = agent;
            m_Path = new List<Vector3>();
            m_NavMeshPath = new NavMeshPath();
            m_NavigationSystem = navigationSystem;

            navigationSystem.OnNavigationMeshChanged += OnNavMeshChanged;
        }

        Vector3 TargetPosition => m_TransformTarget != null  ? m_TransformTarget.position : m_PositionTarget;

        /// <summary>
        /// Set the target of this path to follow a moving transform.
        /// </summary>
        /// <param name="target">The transform to follow.</param>
        public void FollowTransform(Transform target)
        {
            m_TransformTarget = target;
        }

        /// <summary>
        /// Set the target of this path to a static position target.
        /// </summary>
        /// <param name="target">The target position.</param>
        public void SetTargetPosition(Vector3 target)
        {
            // If there is an nav mesh area close to the target use a point inside the nav mesh instead.
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                target = hit.position;
            }

            m_PositionTarget = target;
            m_TransformTarget = null;
            RecalculatePath();
        }

        /// <summary>
        /// Call this to recalculate the path when the navigation mesh or dynamic obstacles changed.
        /// </summary>
        void OnNavMeshChanged()
        {
            RecalculatePath();
        }

        /// <summary>
        /// Clears the path.
        /// </summary>
        public void Clear()
        {
            m_Path.Clear();
        }

        /// <summary>
        /// Gets the movement vector for moving this object while following the path. This function changes the state of the path and should only be called once per tick.
        /// </summary>
        /// <param name="distance">The distance to move.</param>
        /// <returns>Returns the movement vector.</returns>
        public Vector3 MoveAlongPath(float distance)
        {
            if (m_TransformTarget != null)
            {
                OnTargetPositionChanged(TargetPosition);
            }

            if (m_Path.Count == 0)
            {
                return Vector3.zero;
            }

            var currentPredictedPosition = m_Agent.transform.position;
            var remainingDistance = distance;

            while (remainingDistance > 0)
            {
                var toNextPathPoint = m_Path[0] - currentPredictedPosition;

                // If end point is closer then distance to move
                if (toNextPathPoint.sqrMagnitude < remainingDistance * remainingDistance)
                {
                    currentPredictedPosition = m_Path[0];
                    m_Path.RemoveAt(0);
                    remainingDistance -= toNextPathPoint.magnitude;
                }

                // Move towards point
                currentPredictedPosition += toNextPathPoint.normalized * remainingDistance;

                // There is definitely no remaining distance to cover here.
                break;
            }

            return currentPredictedPosition - m_Agent.transform.position;
        }

        void OnTargetPositionChanged(Vector3 newTarget)
        {
            if (m_Path.Count == 0)
            {
                RecalculatePath();
            }

            if ((newTarget - m_CurrentPathOriginalTarget).sqrMagnitude > k_RepathToleranceSqr)
            {
                RecalculatePath();
            }
        }

        /// <summary>
        /// Recalculates the cached navigationPath
        /// </summary>
        void RecalculatePath()
        {
            m_CurrentPathOriginalTarget = TargetPosition;
            m_Agent.CalculatePath(TargetPosition, m_NavMeshPath);

            m_Path.Clear();

            var corners = m_NavMeshPath.corners;

            for (int i = 1; i < corners.Length; i++) // Skip the first corner because it is the starting point.
            {
                m_Path.Add(corners[i]);
            }

            // If the path is still empty here then the target position wasn't on the nav mesh.
            if (m_Path.Count == 0)
            {
                // In that case we just create a linear path directly to the target.
                m_Path.Add(TargetPosition);
            }
        }

        public void Dispose()
        {
            m_NavigationSystem.OnNavigationMeshChanged -= OnNavMeshChanged;
        }
    }
}
