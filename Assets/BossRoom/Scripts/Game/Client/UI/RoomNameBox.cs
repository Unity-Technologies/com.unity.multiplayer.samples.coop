using System;
using UnityEngine;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using UnityEngine.UI;
using VContainer;

public class RoomNameBox : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI m_RoomNameText;
    [SerializeField]
    Button m_CopyToClipboardButton;

    LocalLobby m_LocalLobby;
    string m_LobbyCode;

    [Inject]
    private void InjectDependencies(LocalLobby localLobby)
    {
        m_LocalLobby = localLobby;
        m_LocalLobby.changed += UpdateUI;
        UpdateUI(localLobby);
    }

    void Awake()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        m_LocalLobby.changed -= UpdateUI;
    }

    private void UpdateUI(LocalLobby localLobby)
    {
        if (!string.IsNullOrEmpty(localLobby.LobbyCode))
        {
            m_LobbyCode = localLobby.LobbyCode;
            m_RoomNameText.text = $"Lobby Code: {m_LobbyCode}";
            gameObject.SetActive(true);
            m_CopyToClipboardButton.gameObject.SetActive(true);
        }
    }

    public void CopyToClipboard()
    {
        GUIUtility.systemCopyBuffer = m_LobbyCode;
    }
}
