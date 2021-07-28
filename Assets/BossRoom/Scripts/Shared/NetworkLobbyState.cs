using System;
using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// NetworkBehaviour containing only one NetworkVariable which represents this player's lobby number.
    /// </summary>
    public class NetworkLobbyState : NetworkBehaviour
    {
        NetworkVariableInt m_LobbyNumber = new NetworkVariableInt(
            new NetworkVariableSettings()
            {
                WritePermission = NetworkVariablePermission.ServerOnly
            },
            -1);

        NetworkVariableInt m_ClientCharacterSelection = new NetworkVariableInt(
            new NetworkVariableSettings()
            {
                WritePermission = NetworkVariablePermission.OwnerOnly
            },
            -1);

        NetworkVariableInt m_ServerCharacterSelection = new NetworkVariableInt(
            new NetworkVariableSettings()
            {
                WritePermission = NetworkVariablePermission.ServerOnly
            },
            -1);

        public NetworkVariableInt LobbyNumber => m_LobbyNumber;

        public NetworkVariableInt ClientCharacterSelection => m_ClientCharacterSelection;

        public NetworkVariableInt ServerCharacterSelection => m_ServerCharacterSelection;
    }
}
