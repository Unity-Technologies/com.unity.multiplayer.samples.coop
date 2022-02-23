using System;
using Unity.Multiplayer.Samples.BossRoom;
using UnityEngine;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using UnityEngine.UI;

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

    private void OnDestroy()
    {
        m_LocalLobby.changed -= UpdateUI;
    }

    private void UpdateUI(LocalLobby localLobby)
    {
        switch (localLobby.OnlineMode)
        {
            case OnlineMode.IpHost:
            case OnlineMode.UnityRelay:
                m_LobbyCode = localLobby.LobbyCode;
                m_RoomNameText.text = $"Lobby Code: {m_LobbyCode}";
                m_CopyToClipboardButton.gameObject.SetActive(true);
                break;
            case OnlineMode.Unset:
                //this can happen if we launch the game while circumventing lobby logic
                m_LobbyCode = "";
                m_RoomNameText.text = $"-----------";
                m_CopyToClipboardButton.gameObject.SetActive(false);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void CopyToClipboard()
    {
        GUIUtility.systemCopyBuffer = m_LobbyCode;
    }
}
