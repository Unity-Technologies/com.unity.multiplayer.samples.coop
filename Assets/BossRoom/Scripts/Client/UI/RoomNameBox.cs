using MLAPI;
using MLAPI.Transports;
using MLAPI.Transports.PhotonRealtime;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class RoomNameBox : MonoBehaviour
{

    [SerializeField]
    Text m_RoomNameText;

    void Awake()
    {
        Assert.IsNotNull(m_RoomNameText, $"{nameof(m_RoomNameText)} not assigned!");

        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;

        switch (transport)
        {
            case PhotonRealtimeTransport realtimeTransport:
                m_RoomNameText.text = $"Room Name: {realtimeTransport.RoomName}";
                break;
            default:
                // RoomName should only be displayed when using relay.
                Destroy(gameObject);
                break;
        }
    }
}
