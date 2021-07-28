using UnityEngine;

namespace BossRoom
{
    [CreateAssetMenu]
    public class LobbyPlayerRuntimeCollection : RuntimeCollection<LobbyPlayer>
    {
        public bool TryGetPlayer(ulong clientID, out LobbyPlayer lobbyPlayer)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (clientID == Items[i].OwnerClientId)
                {
                    lobbyPlayer = Items[i];
                    return true;
                }
            }

            lobbyPlayer = null;
            return false;
        }
    }
}
