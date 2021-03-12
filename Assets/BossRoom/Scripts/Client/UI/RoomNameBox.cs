using MLAPI;
using MLAPI.Transports;
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
        if (transport is PhotonRealtimeTransport realtimeTransport)
        {
            m_RoomNameText.text = $"Room Name: {realtimeTransport.RoomName}";
        }
        else
        {
            Destroy(gameObject); // RoomName should only be displayed when using relay.
        }
    }

}
