using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace BossRoom.Scripts.Client.UI
{
    /// <summary>
    /// An individual Lobby in the list of avaialble lobbies
    /// </summary>
    public class LobbyPanelUI : ObserverBehaviour<LocalLobby>
    {
        public UnityEvent<LocalLobby> OnClicked;

        [SerializeField]
        private TextMeshProUGUI m_lobbyNameText;
        [SerializeField]
        private TextMeshProUGUI m_lobbyCountText;
        [SerializeField]
        private TextMeshProUGUI m_OnlineModeText;
        public void OnPanelClicked()
        {
            OnClicked?.Invoke(observed);
        }

        protected override void UpdateObserver(LocalLobby data)
        {
            base.UpdateObserver(data);
            m_lobbyNameText.SetText(data.LobbyName);
            m_lobbyCountText.SetText($"{data.PlayerCount}/{data.MaxPlayerCount}");
            m_OnlineModeText.SetText(data.OnlineMode.ToString());
        }

        public void UpdateLobby(LocalLobby lobby)
        {
            observed.CopyObserved(lobby);
        }
    }
}
