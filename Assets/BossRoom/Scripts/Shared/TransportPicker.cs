using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// This exists as a small helper to easily swap transports while still supporting the UI switch from ip to relay based transports.
/// </summary>
public class TransportPicker : MonoBehaviour
{
    [SerializeField]
    NetworkTransport m_IpHostTransport;

    [SerializeField]
    NetworkTransport m_UnityRelayTransport;

    /// <summary>
    /// The transport used when hosting the game on an IP address.
    /// </summary>
    public NetworkTransport IpHostTransport => m_IpHostTransport;


    /// <summary>
    /// The transport used when hosting the game over a unity relay server.
    /// </summary>
    public NetworkTransport UnityRelayTransport => m_UnityRelayTransport;

    void OnValidate()
    {
        Assert.IsTrue(m_IpHostTransport == null || m_IpHostTransport as UnityTransport,
            "IpHost transport must be UTP.");
    }
}
