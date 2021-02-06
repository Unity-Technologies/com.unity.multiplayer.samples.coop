using MLAPI;
using MLAPI.NetworkedVar;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// Contains all NetworkedVars of a spawner.
    /// This component is present on both client and server objects.
    /// </summary>
    public class NetworkSpawnerState : NetworkedBehaviour
    {
        public NetworkedVarInt HitPoints { get; } = new NetworkedVarInt();
        
        public NetworkedVarBool Broken { get; } = new NetworkedVarBool();
    }
}