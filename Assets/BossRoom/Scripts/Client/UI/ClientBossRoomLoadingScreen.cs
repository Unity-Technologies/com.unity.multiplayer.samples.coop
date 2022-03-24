using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    public class ClientBossRoomLoadingScreen : ClientLoadingScreen
    {
        [SerializeField]
        List<Text> m_OtherPlayerNamesTexts;

        [SerializeField]
        PersistentPlayerRuntimeCollection m_PersistentPlayerRuntimeCollection;

        protected override void UpdateLoadingProgress()
        {
            base.UpdateLoadingProgress();
            if (IsSpawned)
            {
                foreach (var player in m_PersistentPlayerRuntimeCollection.Items)
                {
                    var clientId = player.OwnerClientId;
                    if (clientId != NetworkManager.LocalClientId)
                    {
                        if (!m_ClientIdToProgressBarsIndex.ContainsKey(clientId))
                        {
                            m_ClientIdToProgressBarsIndex[clientId] = m_ClientIdToProgressBarsIndex.Count;
                        }

                        m_OtherPlayerNamesTexts[m_ClientIdToProgressBarsIndex[clientId]].text = player.NetworkNameState.Name.Value;
                    }
                }
            }
        }
    }
}
