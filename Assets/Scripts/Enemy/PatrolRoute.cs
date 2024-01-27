using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PanicBuying
{
    public class PatrolRoute : MonoBehaviour
    {
        public Transform[] patrolPoints;

        private void Start()
        {
            patrolPoints = GetComponentsInChildren<Transform>().Skip(1).ToArray();
        }
    }
}
