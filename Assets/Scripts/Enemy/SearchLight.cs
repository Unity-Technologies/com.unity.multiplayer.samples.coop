using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace PanicBuying
{
    public class SearchLight : NetworkBehaviour
    {
        public TargetFinder patrol;

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("PlayerCharacter"))
            {
                patrol.setTarget(other.gameObject.transform.position);
            }
        }
    }
}
