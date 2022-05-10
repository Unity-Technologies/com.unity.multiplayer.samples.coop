using System;
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

        protected override void AddOtherPlayerProgressBar(ulong clientId)
        {
            base.AddOtherPlayerProgressBar(clientId);
            foreach (var player in m_PersistentPlayerRuntimeCollection.Items)
            {
                if (clientId == player.OwnerClientId)
                {
                    if (m_ClientIdToProgressBarsIndex.ContainsKey(clientId))
                    {
                        m_OtherPlayerNamesTexts[m_ClientIdToProgressBarsIndex[clientId]].text = player.NetworkNameState.Name.Value;
                        m_OtherPlayerNamesTexts[m_ClientIdToProgressBarsIndex[clientId]].gameObject.SetActive(true);
                    }
                    else
                    {
                        throw new Exception("No progress bar is mapped to this player.");
                    }

                    return;
                }
            }
        }

        protected override void RemoveOtherPlayerProgressBar(ulong clientId)
        {
            m_OtherPlayerNamesTexts[m_ClientIdToProgressBarsIndex[clientId]].gameObject.SetActive(false);
            base.RemoveOtherPlayerProgressBar(clientId);
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
