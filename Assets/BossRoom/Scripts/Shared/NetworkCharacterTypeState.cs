using System;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// NetworkBehaviour containing only one NetworkVariable which represents this character's CharacterType.
    /// </summary>
    public class NetworkCharacterTypeState : NetworkBehaviour
    {
        NetworkVariableInt m_CharacterType = new NetworkVariableInt(
            new NetworkVariableSettings()
            {
                WritePermission = NetworkVariablePermission.OwnerOnly
            },
            -1);

        public NetworkVariableInt CharacterType => m_CharacterType;
    }
}
