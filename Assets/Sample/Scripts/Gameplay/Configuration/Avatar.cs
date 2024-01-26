using System;
using Unity.BossRoom.Infrastructure;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.Configuration
{
    /// <summary>
    /// This ScriptableObject defines a Player Character for BossRoom. It defines its CharacterClass field for
    /// associated game-specific properties, as well as its graphics representation.
    /// </summary>
    [CreateAssetMenu]
    [Serializable]
    public sealed class Avatar : GuidScriptableObject
    {
        public CharacterClass CharacterClass;

        public GameObject Graphics;

        public GameObject GraphicsCharacterSelect;

        public Sprite Portrait;
    }
}
