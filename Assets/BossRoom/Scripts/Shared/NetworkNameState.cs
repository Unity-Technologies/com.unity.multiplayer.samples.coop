using Unity.Netcode;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// NetworkBehaviour containing only one NetworkVariableString which represents this object's name.
    /// </summary>
    public class NetworkNameState : NetworkBehaviour
    {
        [HideInInspector]
        public NetworkVariableString Name = new NetworkVariableString();
    }
}
