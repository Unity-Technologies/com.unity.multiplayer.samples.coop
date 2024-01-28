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
        Chasing,
        Standby
    }

    public class TargetFinder : MonoBehaviour
    {
        public float patrolSpeed;
        public float chasingSpeed;
        public float patrolStartTime;

        public List<PatrolRoute> routes;
<<<<<<< Updated upstream
        private NavMeshAgent agent;
        public BoxCaster caster;

        private EnemyState state = EnemyState.Standby;
=======
        protected NavMeshAgent agent;
        public BoxCaster caster;

        protected EnemyState state = EnemyState.Standby;
>>>>>>> Stashed changes

        public int routeIdx;
        public int patrolPointIdx;

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();

            GameObject[] routeObjects = GameObject.FindGameObjectsWithTag("EnemyPatrolRoute");
            foreach (GameObject go in routeObjects)
            {
                routes.Add(go.GetComponent<PatrolRoute>());
            }

            Invoke("StartPatrolling", 3.0f);
        }

        private void Update()
        {
            if (state == EnemyState.Standby) return;
            if ((agent.destination - transform.position).magnitude <= agent.stoppingDistance)
            {
                if (state == EnemyState.Patrolling)
                    Invoke("GetNextPatrolPoint", patrolStartTime);
                else if (state == EnemyState.Chasing)
                    Invoke("StartPatrolling", patrolStartTime);
            }
        }

<<<<<<< Updated upstream
        public void setTarget(Vector3 targetPos)
        {
            caster.transform.LookAt(new Vector3(targetPos.x, 0, targetPos.z));
            caster.BoxCast();            
            if (caster.isHit)
            {
                if (caster.hit.collider.CompareTag("PlayerCharacter"))
=======
        public virtual void setTarget(Vector3 targetPos)
        {
            caster.transform.LookAt(targetPos);
            caster.BoxCast();            
            if (caster.isHit)
            {
>>>>>>> Stashed changes
                {
                    state = EnemyState.Chasing;
                    agent.speed = chasingSpeed;
                    agent.SetDestination(targetPos);
                }
            }
        }

        private void StartPatrolling()
        {
            routeIdx = 0;
            patrolPointIdx = 0;
            float shortestDistance = float.MaxValue;

            // Find closest route
            for (int i=0; i<routes.Count; i++)
            {
                for (int j=0; j < routes[i].patrolPoints.Length; j++)
                {
                    if (shortestDistance > (routes[i].patrolPoints[j].position - transform.position).magnitude)
                    {
                        routeIdx = i;
                        patrolPointIdx = j;
                        shortestDistance = (routes[i].patrolPoints[j].position - transform.position).magnitude;
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

<<<<<<< Updated upstream
        private void SetPatrolPoint(int routeIdx, int patrolPointIdx)
=======
        protected virtual void SetPatrolPoint(int routeIdx, int patrolPointIdx)
>>>>>>> Stashed changes
        {
            CancelInvoke();
            state = EnemyState.Patrolling;
            agent.speed = patrolSpeed;

            agent.SetDestination(routes[routeIdx].patrolPoints[patrolPointIdx].position);
        }
    }
}
