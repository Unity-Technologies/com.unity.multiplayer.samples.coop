using System;
using MLAPI;
using MLAPI.NetworkedVar;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// MonoBehaviour containing only one NetworkedVar which represents this character's CharacterType.
    /// </summary>
    public class NetworkCharacterTypeState : NetworkedBehaviour
    {
        [Tooltip("NPCs should set this value in their prefab. For players, this value is set at runtime.")]
        public NetworkedVar<CharacterTypeEnum> CharacterType = new NetworkedVar<CharacterTypeEnum>();
    }
}
