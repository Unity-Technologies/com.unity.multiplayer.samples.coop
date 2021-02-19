using UnityEngine;
using MLAPI.NetworkedVar;

namespace BossRoom
{
    public abstract class NetworkUtils
    {
        public static NetworkedVarSettings ServerWriteEveryoneReadPermission = new NetworkedVarSettings
        {
            WritePermission = NetworkedVarPermission.ServerOnly,
            ReadPermission = NetworkedVarPermission.Everyone
        };
    }
}
