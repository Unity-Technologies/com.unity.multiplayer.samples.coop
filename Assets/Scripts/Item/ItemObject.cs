using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace PanicBuying
{
    public class ItemObject_Holding : MonoBehaviour
    {
        virtual public bool Use()
        {
            return true;
        }

        NetworkInventory parentInventory;
    }

    [RequireComponent(typeof(NetworkObject))]
    public class ItemObject_Dropped : NetworkBehaviour
    {
        public ItemData itemData = new();

        private void Awake()
        {
            networkObject.Spawn();
        }

        [SerializeField]
        NetworkObject networkObject;
    }
}
