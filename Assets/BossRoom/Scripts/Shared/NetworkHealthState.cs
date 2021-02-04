using MLAPI;
using MLAPI.NetworkedVar;
using UnityEngine;

namespace BossRoom
{
    public class NetworkHealthState : NetworkedBehaviour
    {
        [HideInInspector]
        public NetworkedVarInt HitPoints;
    }
}
