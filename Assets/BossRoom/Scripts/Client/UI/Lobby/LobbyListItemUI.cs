using TMPro;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// An individual Lobby UI in the list of available lobbies
    /// </summary>
    public class LobbyListItemUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI m_lobbyNameText;
        [SerializeField] TextMeshProUGUI m_lobbyCountText;

        LobbyUIMediator m_LobbyUIMediator;
        LocalLobby m_Data;

        [Inject]
        void InjectDependencies(LobbyUIMediator lobbyUIMediator)
        {
            m_LobbyUIMediator = lobbyUIMediator;
        }

        public void SetData(LocalLobby data)
        {
            m_Data = data;
            m_lobbyNameText.SetText(data.LobbyName);
            m_lobbyCountText.SetText($"{data.PlayerCount}/{data.MaxPlayerCount}");
        }

        public void OnClick()
        {
            m_LobbyUIMediator.JoinLobbyRequest(m_Data);
        }
    }
}
