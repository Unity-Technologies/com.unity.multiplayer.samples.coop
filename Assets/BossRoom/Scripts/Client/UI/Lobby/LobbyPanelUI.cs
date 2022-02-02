using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using TMPro;
using UnityEngine;

namespace BossRoom.Scripts.Client.UI
{
    /// <summary>
    ///     An individual Lobby in the list of avaialble lobbies
    /// </summary>
    public class LobbyPanelUI : ObserverBehaviour<LocalLobby>
    {
        [SerializeField] private TextMeshProUGUI m_lobbyNameText;
        [SerializeField] private TextMeshProUGUI m_lobbyCountText;
        [SerializeField] private TextMeshProUGUI m_OnlineModeText;
        private LobbyUIMediator m_LobbyUIMediator;

        [Inject]
        private void InjectDependencies(LobbyUIMediator lobbyUIMediator)
        {
            m_LobbyUIMediator = lobbyUIMediator;
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
            Observed.CopyObserved(lobby);
        }

        public void OnClick()
        {
            m_LobbyUIMediator.JoinLobbyRequest(Observed.Data);
        }
    }
}
