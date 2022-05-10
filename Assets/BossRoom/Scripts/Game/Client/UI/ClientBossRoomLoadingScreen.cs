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
        protected class BossRoomLoadingProgressBar : LoadingProgressBar
        {
            public Text NameText { get; set; }

            public BossRoomLoadingProgressBar(Slider otherPlayerProgressBar, Text otherPlayerNameText)
                : base(otherPlayerProgressBar)
            {
                NameText = otherPlayerNameText;
            }
        }

        [SerializeField]
        List<Text> m_OtherPlayerNamesTexts;

        [SerializeField]
        PersistentPlayerRuntimeCollection m_PersistentPlayerRuntimeCollection;

        protected override void AddOtherPlayerProgressBar(ulong clientId, NetworkedLoadingProgressTracker progressTracker)
        {
            foreach (var player in m_PersistentPlayerRuntimeCollection.Items)
            {
                if (clientId == player.OwnerClientId)
                {
                    if (m_LoadingProgressBars.Count < m_OtherPlayersProgressBars.Count)
                    {
                        var index = m_LoadingProgressBars.Count;
                        m_LoadingProgressBars[clientId] = new BossRoomLoadingProgressBar(m_OtherPlayersProgressBars[index], m_OtherPlayerNamesTexts[index]);
                        progressTracker.Progress.OnValueChanged += m_LoadingProgressBars[clientId].UpdateProgress;
                        m_LoadingProgressBars[clientId].ProgressBar.gameObject.SetActive(true);
                    }
                    else
                    {
                        throw new Exception("There are not enough progress bars to track the progress of all the players.");
                    }

                    return;
                }
            }
        }

        protected override void RemoveOtherPlayerProgressBar(ulong clientId, NetworkedLoadingProgressTracker progressTracker = null)
        {
            ((BossRoomLoadingProgressBar) m_LoadingProgressBars[clientId]).NameText.gameObject.SetActive(false);
            base.RemoveOtherPlayerProgressBar(clientId, progressTracker);
        }

        protected override void UpdateOtherPlayerProgressBar(ulong clientId, int progressBarIndex)
        {
            ((BossRoomLoadingProgressBar) m_LoadingProgressBars[clientId]).NameText = m_OtherPlayerNamesTexts[progressBarIndex];
            base.UpdateOtherPlayerProgressBar(clientId, progressBarIndex);
        }
    }
}
