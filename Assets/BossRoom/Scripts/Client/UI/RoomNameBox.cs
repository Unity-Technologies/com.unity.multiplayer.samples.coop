using MLAPI;
using MLAPI.Transports;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;

public class RoomNameBox : MonoBehaviour
{

    [SerializeField]
    TextMeshProUGUI m_RoomNameText;

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
