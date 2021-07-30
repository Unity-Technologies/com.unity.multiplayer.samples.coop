using System;
using UnityEngine;

namespace BossRoom.Client
{
    /// <summary>
    /// A runtime list of <see cref="PersistentPlayer"/> objects that is populated both on clients and server.
    /// </summary>
    [CreateAssetMenu]
    public class ClientPlayerAvatarRuntimeCollection : RuntimeCollection<ClientPlayerAvatar>
    {
        public bool TryGetPlayer(ulong clientID, out ClientPlayerAvatar clientPlayerAvatar)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (clientID == Items[i].OwnerClientId)
                {
                    clientPlayerAvatar = Items[i];
                    return true;
                }
            }

            clientPlayerAvatar = null;
            return false;
        }
    }
}
