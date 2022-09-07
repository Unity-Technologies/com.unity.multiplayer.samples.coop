using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character
{
    [RequireComponent(typeof(NetworkCharacterState))]
    public class ClientCharacter : NetworkBehaviour
    {
        [SerializeField]
        ClientCharacterVisualization m_ClientCharacterVisualization;

        /// <summary>
        /// The Visualization GameObject isn't in the same transform hierarchy as the object, but it registers itself here
        /// so that the visual GameObject can be found from a NetworkObjectId.
        /// </summary>
        public ClientCharacterVisualization ChildVizObject => m_ClientCharacterVisualization;

        public override void OnNetworkSpawn()
        {
            if (!IsClient)
            {
                enabled = false;
            }
        }
    }
}
