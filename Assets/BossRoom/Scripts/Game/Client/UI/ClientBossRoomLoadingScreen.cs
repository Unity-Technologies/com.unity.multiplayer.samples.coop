using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
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

        protected override void UpdateProgressBars(bool isInitializing)
        {
            base.UpdateProgressBars(isInitializing);
            foreach (var player in m_PersistentPlayerRuntimeCollection.Items)
            {
                var clientId = player.OwnerClientId;
                if (clientId != NetworkManager.Singleton.LocalClientId)
                {
                    if (m_ClientIdToProgressBarsIndex.ContainsKey(clientId))
                    {
                        m_OtherPlayerNamesTexts[m_ClientIdToProgressBarsIndex[clientId]].text = player.NetworkNameState.Name.Value;
                        m_OtherPlayerNamesTexts[m_ClientIdToProgressBarsIndex[clientId]].gameObject.SetActive(true);
                    }
                }
            }
        }

        protected override void ReinitializeProgressBars()
        {
            // deactivate all other players' name text
            foreach (var playerName in m_OtherPlayerNamesTexts)
            {
                playerName.gameObject.SetActive(false);
            }
            base.ReinitializeProgressBars();
        }
    }
}
