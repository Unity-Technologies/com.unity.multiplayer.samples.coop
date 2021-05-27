using System;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// A runtime list of <see cref="BossRoomPlayer"/> objects that is populated both on clients and server.
    /// </summary>
    [CreateAssetMenu]
    public class BossRoomPlayerRuntimeCollection : RuntimeCollection<BossRoomPlayer>
    {
        public bool TryGetPlayer(ulong clientID, out BossRoomPlayer bossRoomPlayer)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (clientID == Items[i].OwnerClientId)
                {
                    bossRoomPlayer = Items[i];
                    return true;
                }
            }

            bossRoomPlayer = null;
            return false;
        }
    }
}
