using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    public class ClientTossedItem : NetworkBehaviour
    {
        public UnityEvent detonatedCallback;

        public void Detonate()
        {
            detonatedCallback?.Invoke();
        }
    }
}
