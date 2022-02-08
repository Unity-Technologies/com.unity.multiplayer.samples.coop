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

    string m_RoomName;

    bool m_ConnectionFinished = false;


    [Inject]
    private void InjectDependencies(LocalLobby localLobby)
    {
        BeginObserving(localLobby);
    }

    protected override void UpdateObserver(LocalLobby localLobby)
    {
        //todo: move the Update logic to here
        switch (localLobby.OnlineMode)
        {
            case OnlineMode.IpHost:
                m_RoomNameText.text = localLobby.LobbyCode;
                break;
            case OnlineMode.UnityRelay:
                m_RoomNameText.text = localLobby.RelayCode;
                break;
            case OnlineMode.Unset:
                //this can happen if we launch the game while circumventing lobby logic
                m_RoomNameText.text = $"-----------";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    // This update loop exists because there is currently a bug in Netcode for GameObjects which runs client connected callbacks before the transport has
    // fully finished the asynchronous connection. That's why are loading the character select screen too early and need this update loop to
    // update the room key once we are fully connected to the Photon cloud.
    void Update()
    {
        if (m_ConnectionFinished == false)
        {
            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;

            if (transport != null && transport is UnityTransport utp &&
                     !string.IsNullOrEmpty(UnityRelayUtilities.JoinCode))
            {
                ConnectionFinished(UnityRelayUtilities.JoinCode);
            }
        }
    }

    void ConnectionFinished(string roomName)
    {
        m_RoomName = roomName;
        m_RoomNameText.text = $"Room Name: {m_RoomName}";
        m_ConnectionFinished = true;
        m_CopyToClipboardButton.gameObject.SetActive(true);
    }

    public void CopyToClipboard()
    {
        if (m_ConnectionFinished)
        {
            GUIUtility.systemCopyBuffer = m_RoomName;
        }
        else
        {
            Debug.Log("Connection not finished, can't copy to clipboard yet.");
        }
    }
}
