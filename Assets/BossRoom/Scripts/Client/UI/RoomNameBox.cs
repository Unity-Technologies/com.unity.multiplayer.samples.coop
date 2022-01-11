using Unity.Multiplayer.Samples.BossRoom;
using Netcode.Transports.PhotonRealtime;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;

public class RoomNameBox : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI m_RoomNameText;
    [SerializeField]
    Button m_CopyToClipboardButton;

    string m_RoomName;

    bool m_ConnectionFinished = false;

    void Awake()
    {
        Assert.IsNotNull(m_RoomNameText, $"{nameof(m_RoomNameText)} not assigned!");

        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        bool isUsingRelay = true;
        switch (transport)
        {
            case PhotonRealtimeTransport realtimeTransport:
                m_RoomNameText.text = $"Loading room key...";
                break;
            case UnityTransport utp:
                if (utp.Protocol == UnityTransport.ProtocolType.RelayUnityTransport)
                {
                    m_RoomNameText.text = $"Loading join code...";
                }
                else
                {
                    isUsingRelay = false;
                }
                break;
            default:
                isUsingRelay = false;
                break;
        }

        if (!isUsingRelay)
        {
            // RoomName should only be displayed when using relay.
            Destroy(gameObject);
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

            if (transport != null &&
                transport is PhotonRealtimeTransport realtimeTransport &&
                realtimeTransport.Client != null &&
                string.IsNullOrEmpty(realtimeTransport.Client.CloudRegion) == false)
            {
                string roomName = $"{realtimeTransport.Client.CloudRegion.ToUpper()}_{realtimeTransport.RoomName}";
                ConnectionFinished(roomName);
            }
            else if (transport != null && transport is UnityTransport utp &&
                     !string.IsNullOrEmpty(RelayJoinCode.Code))
            {
                ConnectionFinished(RelayJoinCode.Code);
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
