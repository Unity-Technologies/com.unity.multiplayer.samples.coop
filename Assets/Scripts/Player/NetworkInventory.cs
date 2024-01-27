using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;

namespace PanicBuying
{
    public class NetworkInventory : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsClient)
            {
                RequestInventory_ServerRpc();
            }
        }

        public void SelectSlot(int InIndex)
        {
            Debug.Assert(InIndex >= 0 && InIndex < items.Count);

            selectedItem = items[InIndex];
            if (selectedItem != null)
            {
                selectedItem.transform.SetParent(handObject);
            }
        }

        public void Drop()
        {
            if (IsOwner && selectedItem)
            {
                NetworkObject Object = Instantiate(itemObjectPrefab).GetComponent<NetworkObject>();

                if (selectedItem.Count > 1)
                {
                    selectedItem.RemoveOne();
                }
                else
                {
                    selectedItem.transform.SetParent(Object.transform);
                    items.Remove(selectedItem);
                }


                Object.Spawn();
            }
        }

        bool TryGetItem(ItemBehaviour InItem)
        {
            foreach (var ExistingItem in items)
            {
                if (ExistingItem.Accumulate(InItem) == false)
                {
                    continue;
                }
            }

            if (InItem.Count > 0 && items.Count < kItemSlotNum)
            {
                InItem.transform.SetParent(transform);
                items.Add(InItem);
                return true;
            }
            else if (InItem.Count <= 0)
            {
                Destroy(InItem.gameObject);
                return true;
            }
            else
            {
                return false;
            }
        }


        [ServerRpc]
        void RequestInventory_ServerRpc(ServerRpcParams serverRpcParams = new())
        {
            if (serverRpcParams.Receive.SenderClientId == OwnerClientId)
            {
                var Params = PanicUtil.MakeClientRpcParams(OwnerClientId);

                SendInventory_ClientRpc(new (items), Params);
            }
        }

        [ClientRpc]
        void SendInventory_ClientRpc(InventoryStruct inventory, ClientRpcParams clientRpcParams)
        {
            items = new(inventory.itemBehaviours);
        }


        const int kItemSlotNum = 4;

        [SerializeField]
        NetworkObject owner;

        [SerializeField]
        Transform handObject;

        [SerializeField]
        NetworkObject itemObjectPrefab;

        ItemBehaviour selectedItem = null;

        [SerializeField, HideInInspector]
        protected List<ItemBehaviour> items = new();
    }

    public struct InventoryStruct : INetworkSerializeByMemcpy
    {
        public InventoryStruct(List<ItemBehaviour> inItems)
        {
            itemBehaviours = inItems.ToArray();
        }

        public ItemBehaviour[] itemBehaviours;
    }
}
