using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PanicBuying
{
    public class BoxCaster : MonoBehaviour
    {
        public float maxDistance;
        public RaycastHit hit;
        public bool isHit;

        public Transform finder;

        void OnDrawGizmos()
        {
            BoxCast();
            Gizmos.color = Color.red;
            if (isHit)
            {
                Gizmos.DrawRay(transform.position + new Vector3(0, transform.lossyScale.y / 2, 0), transform.forward * hit.distance);
            }
            else
            {
                Gizmos.DrawRay(transform.position + new Vector3(0, transform.lossyScale.y / 2, 0), transform.forward * maxDistance);
            }
        }

        public void BoxCast()
        {
            isHit = Physics.Raycast(transform.position + new Vector3(0, transform.lossyScale.y / 2, 0), transform.forward, out hit, maxDistance);
        }
    }
}
