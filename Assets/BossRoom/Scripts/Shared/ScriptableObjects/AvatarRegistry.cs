using System;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// This ScriptableObject will be the container for all possible Avatars inside BossRoom.
    /// <see cref="Avatar"/>
    /// </summary>
    [CreateAssetMenu]
    public class AvatarRegistry : ScriptableObject
    {
        [SerializeField]
        Avatar[] m_Avatars;

        public bool TryGetAvatar(Guid guid, out Avatar avatarValue)
        {
            avatarValue = Array.Find(m_Avatars, avatar => avatar.Guid == guid);

            return avatarValue != null;
        }
    }
}
