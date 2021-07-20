using System;
using MLAPI;
using UnityEngine;

namespace BossRoom.Client
{
    public class ClientPlayerAvatar : NetworkBehaviour
    {
        public static event Action<ClientPlayerAvatar> LocalClientSpawned;

        public static event Action LocalClientDespawned;

        public override void OnNetworkSpawn()
        {
            name = "PlayerAvatar" + OwnerClientId;

            if (IsClient && IsOwner)
            {
                LocalClientSpawned?.Invoke(this);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsClient && IsOwner)
            {
                LocalClientDespawned?.Invoke();
            }
        }
    }
}
