using System;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// A runtime list of <see cref="PersistentPlayer"/> objects that is populated both on clients and server.
    /// </summary>
    [CreateAssetMenu]
    public class NetworkCharacterStateRuntimeCollection : RuntimeCollection<NetworkCharacterState>
    {
        public bool TryGetPlayer(ulong clientID, out NetworkCharacterState networkCharacterState)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (clientID == Items[i].OwnerClientId)
                {
                    networkCharacterState = Items[i];
                    return true;
                }
            }

            networkCharacterState = null;
            return false;
        }
    }
}
