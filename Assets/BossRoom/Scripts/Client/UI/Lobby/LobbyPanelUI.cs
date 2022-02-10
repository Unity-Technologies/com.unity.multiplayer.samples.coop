using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom;
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
        [SerializeField] private TextMeshProUGUI m_CodesText;
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
            switch (data.OnlineMode)
            {
                case OnlineMode.IpHost:
                case OnlineMode.Unset:
                    m_CodesText.text = "";
                    break;
                case OnlineMode.UnityRelay:
                    m_CodesText.text = $"Relay Join Code: {data.RelayJoinCode}";
                    break;
            }
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
