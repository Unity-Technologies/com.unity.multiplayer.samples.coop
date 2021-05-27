using System;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// A runtime list of <see cref="BossRoomPlayerCharacter"/> objects that is populated both on clients and server.
    /// </summary>
    [CreateAssetMenu]
    public class BossRoomPlayerCharacterRuntimeCollection : RuntimeCollection<BossRoomPlayerCharacter>
    {
        public bool TryGetPlayerCharacter(ulong clientID, out BossRoomPlayerCharacter bossRoomPlayerCharacter)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (clientID == Items[i].OwnerClientId)
                {
                    bossRoomPlayerCharacter = Items[i];
                    return true;
                }
            }

            bossRoomPlayerCharacter = null;
            return false;
        }
    }
}
