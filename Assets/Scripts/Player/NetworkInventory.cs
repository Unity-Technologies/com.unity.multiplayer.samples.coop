using System;
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

        bool SetHoldingItem(ItemSO inSO)
        {
            if (inSO == null && holdingObject == null)
                return false;

            if (holdingObject)
                Destroy(holdingObject);

            if (inSO != null)
            {
                holdingObject = Instantiate(inSO.HoldingPrefab).GetComponent<ItemObject_Holding>();
                holdingObject.transform.SetParent(handTransform);
            }

            return true;
        }

        bool SelectSlot(int inIndex = -1)
        {
            if (inIndex == -1)
            {
                selectedIndex = inIndex;

                return SetHoldingItem(null);
            }

            inIndex = Math.Clamp(inIndex, 0, items.Count - 1);

            bool sameSO = IsSelecting() && items[selectedIndex].SO == items[inIndex].SO;
            selectedIndex = inIndex;

            if (sameSO)
                return false;

            return SetHoldingItem(items[selectedIndex].SO);
        }

        [ServerRpc]
        public void SelectSlot_ServerRpc(int inIndex, ServerRpcParams serverRpcParams = new())
        {
            if (SelectSlot(inIndex))
            {
                SelectItem_ClientRpc(inIndex, items[inIndex].Id);
            }
        }

        [ClientRpc]
        void SelectItem_ClientRpc(int inIndex, int inItemId, ClientRpcParams clientRpcParams = new())
        {
            if (IsOwner)
                SelectSlot(inIndex);
            else
                SetHoldingItem(GameManager.Instance?.GetItemSO(inItemId));
        }

        [ServerRpc]
        public void Drop_ServerRpc(ServerRpcParams serverRpcParams = new())
        {
            if (serverRpcParams.Receive.SenderClientId != OwnerClientId)
                return;

            if (IsOwner && holdingObject && IsSelecting())
            {
                ItemObject_Dropped Object = Instantiate(itemObjectPrefab).GetComponent<ItemObject_Dropped>();

                if (items[selectedIndex].Count > 1)
                {
                    items[selectedIndex].RemoveOne();
                }
                else
                {
                    if (SelectSlot(-1))
                    {
                        SelectItem_ClientRpc(-1, -1);
                    }

                    RemoveItem_ClientRpc(items[selectedIndex], PanicUtil.MakeClientRpcParams(OwnerClientId));
                    items.RemoveAt(selectedIndex);
                }
            }
        }

        bool TryGetItem(ItemObject_Dropped inItemObject)
        {
            for (int i = 0; i < items.Count; ++i)
            {
                if (items[i].Accumulate(ref inItemObject.itemData))
                {
                    UpdateItem_ClientRpc(i, items[i], PanicUtil.MakeClientRpcParams(OwnerClientId));
                }
            }

            if (inItemObject.itemData.Count > 0 && items.Count < kItemSlotNum)
            {
                AddItem_ClientRpc(inItemObject.itemData, PanicUtil.MakeClientRpcParams(OwnerClientId));
                items.Add(inItemObject.itemData);

                Destroy(inItemObject.gameObject);
                return true;
            }
            else if (inItemObject.itemData.Count <= 0)
            {
                Destroy(inItemObject.gameObject);
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
            if (serverRpcParams.Receive.SenderClientId != OwnerClientId)
                return;

            var Params = PanicUtil.MakeClientRpcParams(OwnerClientId);

            SendInventory_ClientRpc(new(items), Params);
        }

        [ClientRpc]
        void SendInventory_ClientRpc(InventoryStruct inventory, ClientRpcParams clientRpcParams)
        {
            items = new(inventory.itemBehaviours);
        }

        [ClientRpc]
        void RemoveItem_ClientRpc(ItemData inItem, ClientRpcParams clientRpcParams)
        {
            if (items.Remove(inItem) == false)
            {
                RequestInventory_ServerRpc();
            }
        }

        [ClientRpc]
        void AddItem_ClientRpc(ItemData inItem, ClientRpcParams clientRpcParams)
        {
            if(items.Count < kItemSlotNum)
            {
                items.Add(inItem);
            }
            else
            {
                RequestInventory_ServerRpc();
            }
        }

        [ClientRpc]
        void UpdateItem_ClientRpc(int slotIndex, ItemData inItem, ClientRpcParams clientRpcParams)
        {
            if (slotIndex >= 0 && slotIndex < items.Count && items[slotIndex].SO == inItem.SO)
            {
                items[slotIndex] = inItem;
            }
            else
            {
                RequestInventory_ServerRpc();
            }
        }

        const int kItemSlotNum = 4;

        [SerializeField]
        Transform handTransform;

        ItemObject_Holding holdingObject;

        [SerializeField]
        NetworkObject itemObjectPrefab;

        int selectedIndex = -1;
        bool IsSelecting() { return selectedIndex >= 0 && selectedIndex < items.Count; }

        [SerializeField, HideInInspector]
        protected List<ItemData> items = new();
    }

    public struct InventoryStruct : INetworkSerializeByMemcpy
    {
        public InventoryStruct(List<ItemData> inItems)
        {
            itemBehaviours = inItems.ToArray();
        }

        public ItemData[] itemBehaviours;
    }
}
