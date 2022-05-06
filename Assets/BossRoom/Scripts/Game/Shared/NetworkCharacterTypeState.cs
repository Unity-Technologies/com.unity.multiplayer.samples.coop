using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// NetworkBehaviour containing only one NetworkVariable which represents this character's CharacterType.
    /// </summary>
    public class NetworkCharacterTypeState : NetworkBehaviour
    {
        [Tooltip("NPCs should set this value in their prefab. For players, this value is set at runtime.")]
        public NetworkVariable<CharacterTypeEnum> CharacterType = new NetworkVariable<CharacterTypeEnum>();
    }
}
