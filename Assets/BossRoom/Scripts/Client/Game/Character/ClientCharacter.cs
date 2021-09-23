using Unity.Netcode;
using UnityEngine;
using Unity.Multiplayer.Samples.BossRoom.Visual;

namespace Unity.Multiplayer.Samples.BossRoom.Client
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

        public void SetCharacterVisualization(ClientCharacterVisualization clientCharacterVisualization)
        {
            m_ClientCharacterVisualization = clientCharacterVisualization;
        }
    }
}
