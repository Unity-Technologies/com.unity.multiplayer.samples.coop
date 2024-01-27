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


        [ServerRpc]
        void RequestInventory_ServerRpc(ServerRpcParams serverRpcParams = new())
        {
            if (serverRpcParams.Receive.SenderClientId == OwnerClientId)
            {
                var Params = PanicUtil.MakeClientRpcParams(OwnerClientId);

                SendInventory_ClientRpc(_inventory, Params);
            }
        }

        [ClientRpc]
        void SendInventory_ClientRpc(InventoryStruct inventory, ClientRpcParams clientRpcParams)
        {
            _inventory = inventory;
        }


        protected InventoryStruct _inventory;
    }


    public struct InventoryStruct : INetworkSerializeByMemcpy
    {
        public InventoryStruct(int SlotNumber = 5)
        {
            Items = new(SlotNumber, Allocator.Persistent);
        }

        NativeArray<ItemData> Items;
    }
}
