using System;
using Unity.Netcode;

namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    public class NetworkDoorState : NetworkBehaviour
    {
        /// <summary>
        /// Network state for a door which can be opened by pressing on a floor switch
        /// </summary>
        public NetworkVariable<bool> IsOpen { get; } = new NetworkVariable<bool>();
    }
}
