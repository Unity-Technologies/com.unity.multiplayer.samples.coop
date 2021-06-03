using System;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// This ScriptableObject will be the container for all possible Player Characters inside BossRoom.
    /// <see cref="Character"/>
    /// </summary>
    [CreateAssetMenu]
    public class CharacterRegistry : ScriptableObject
    {
        [SerializeField]
        Character[] m_Characters;

        public bool TryGetCharacter(Guid guid, out Character characterValue)
        {
            characterValue = Array.Find(m_Characters, character => character.Guid == guid);

            return characterValue != null;
        }
    }
}
