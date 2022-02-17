using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using TMPro;
using UnityEngine;

namespace BossRoom.Scripts.Client.UI
{
    /// <summary>
    /// An individual Lobby UI in the list of available lobbies
    /// </summary>
    public class LobbyListItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_lobbyNameText;
        [SerializeField] private TextMeshProUGUI m_lobbyCountText;
        [SerializeField] private TextMeshProUGUI m_OnlineModeText;

        private LobbyUIMediator m_LobbyUIMediator;
        private LocalLobby m_Data;

        [Inject]
        private void InjectDependencies(LobbyUIMediator lobbyUIMediator)
        {
            m_LobbyUIMediator = lobbyUIMediator;
        }

        public void SetData(LocalLobby data)
        {
            m_Data = data;
            m_lobbyNameText.SetText(data.LobbyName);
            m_lobbyCountText.SetText($"{data.PlayerCount}/{data.MaxPlayerCount}");
            m_OnlineModeText.SetText(data.OnlineMode.ToString());
        }

        public void OnClick()
        {
            m_LobbyUIMediator.JoinLobbyRequest(m_Data);
        }
    }
}
