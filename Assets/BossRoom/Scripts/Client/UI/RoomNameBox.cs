using Unity.Multiplayer.Samples.BossRoom;
using Netcode.Transports.PhotonRealtime;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;
using Unity.Netcode;

public class RoomNameBox : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI m_RoomNameText;

    bool m_ConnectionFinished = false;

    void Awake()
    {
        Assert.IsNotNull(m_RoomNameText, $"{nameof(m_RoomNameText)} not assigned!");

        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;

        switch (transport)
        {
            case PhotonRealtimeTransport realtimeTransport:
                m_RoomNameText.text = $"Loading room key...";
                break;
            case UnityTransport utp:
                m_RoomNameText.text = $"Loading join code...";
                break;
            default:
                // RoomName should only be displayed when using relay.
                Destroy(gameObject);
                break;
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
                m_RoomNameText.text = $"Room Name: {roomName}";
                m_ConnectionFinished = true;
            }
            else if (transport != null && transport is UnityTransport utp &&
                     !string.IsNullOrEmpty(RelayJoinCode.Code))
            {
                m_RoomNameText.text = RelayJoinCode.Code;
                m_ConnectionFinished = true;
            }
        }
    }
}
