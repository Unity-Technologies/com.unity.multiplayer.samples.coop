using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.AI;

namespace PanicBuying
{
    public enum EnemyState
    {
        Patrolling,
        Chasing
    }

    public class TargetFinder : MonoBehaviour
    {
        public float patrolSpeed;
        public float chasingSpeed;
        public float patrolStartTime;

        public List<PatrolRoute> routes;
        private NavMeshAgent agent;

        private EnemyState state = EnemyState.Chasing;

        private int routeIdx;
        private int patrolPointIdx;

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();

            GameObject[] routeObjects = GameObject.FindGameObjectsWithTag("EnemyPatrolRoute");
            foreach (GameObject go in routeObjects)
            {
                routes.Add(go.GetComponent<PatrolRoute>());
            }

            StartPatrolling();
        }

        private void Update()
        {
            Debug.Log(agent.remainingDistance);
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (state == EnemyState.Patrolling)
                    Invoke("GetNextPatrolPoint", patrolStartTime);
                else if (state == EnemyState.Chasing)
                    Invoke("StartPatrolling", patrolStartTime);
            }
        }

        public void setTarget(Vector3 targetPos)
        {
            CancelInvoke();
            state = EnemyState.Chasing;
            agent.speed = chasingSpeed;
            agent.SetDestination(targetPos);
        }

        private void StartPatrolling()
        {
            CancelInvoke();
            state = EnemyState.Patrolling;
            agent.speed = patrolSpeed;
            routeIdx = 0;
            patrolPointIdx = 0;
            float shortestDistance = float.MaxValue;

            // Find closest route
            for (int i=0; i<routes.Count; i++)
            {
                for (int j=0; j < routes[i].patrolPoints.Length; j++)
                {
                    agent.SetDestination(routes[i].patrolPoints[j].position);
                    if (shortestDistance > agent.remainingDistance)
                    {
                        routeIdx = i;
                        patrolPointIdx = j;
                        shortestDistance = agent.remainingDistance;
                    }
                }
            }

            SetPatrolPoint(routeIdx, patrolPointIdx);
        }

        private void GetNextPatrolPoint()
        {
            patrolPointIdx++;
            if (patrolPointIdx >= routes[routeIdx].patrolPoints.Length)
            {
                patrolPointIdx = 0;
            }

            SetPatrolPoint(routeIdx, patrolPointIdx);
        }

        private void SetPatrolPoint(int routeIdx, int patrolPointIdx)
        {
            CancelInvoke();
            agent.SetDestination(routes[routeIdx].patrolPoints[patrolPointIdx].position);
        }
    }
}
