using System;
using Unity.Netcode;
using UnityEngine;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Multiplayer.Samples.Utilities;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    // TODO this whole class seems unnecessary, it might be worth it to put this ref somewhere else.
    [RequireComponent(typeof(NetworkCharacterState))]
    public class ClientCharacter : MonoBehaviour
    {
        [SerializeField]
        ClientCharacterVisualization m_ClientCharacterVisualization;

        /// <summary>
        /// The Visualization GameObject isn't in the same transform hierarchy as the object, but it registers itself here
        /// so that the visual GameObject can be found from a NetworkObjectId.
        /// </summary>
        public ClientCharacterVisualization ChildVizObject => m_ClientCharacterVisualization;

        void Awake()
        {
            GetComponent<NetcodeHooks>().OnNetworkSpawnHook += OnSpawn;
        }

        public void OnSpawn()
        {
            if (!NetworkManager.Singleton.IsClient)
            {
                enabled = false;
            }
        }

        void OnDestroy()
        {
            GetComponent<NetcodeHooks>().OnNetworkSpawnHook -= OnSpawn;
        }
    }
}
