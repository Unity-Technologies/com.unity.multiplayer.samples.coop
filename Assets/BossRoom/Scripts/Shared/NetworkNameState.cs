using MLAPI;
using MLAPI.NetworkedVar;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// NetworkedBehaviour containing only one NetworkedVarString which represents this object's name.
    /// </summary>
    public class NetworkNameState : NetworkedBehaviour
    {
        [HideInInspector]
        public NetworkedVarString Name = new NetworkedVarString();
    }
}
