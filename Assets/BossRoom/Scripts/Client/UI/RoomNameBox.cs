using System;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.BossRoom;
using Netcode.Transports.PhotonRealtime;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;

public class RoomNameBox : ObserverBehaviour<LocalLobby>
{
    [SerializeField]
    TextMeshProUGUI m_RoomNameText;
    [SerializeField]
    Button m_CopyToClipboardButton;

    string m_LobbyCode;

    [Inject]
    private void InjectDependencies(LocalLobby localLobby)
    {
        BeginObserving(localLobby);
    }

    protected override void UpdateObserver(LocalLobby localLobby)
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
