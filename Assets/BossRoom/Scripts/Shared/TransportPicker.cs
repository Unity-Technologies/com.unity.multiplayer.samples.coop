using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// This exists as a small helper to easily swap transports while still supporting the UI switch from ip to relay based transports.
/// </summary>
public class TransportPicker : MonoBehaviour
{
    [SerializeField]
    UnityTransport m_IpHostTransport;

    [SerializeField]
    UnityTransport m_UnityRelayTransport;

    /// <summary>
    /// The transport used when hosting the game on an IP address.
    /// </summary>
    public UnityTransport IpHostTransport => m_IpHostTransport;


    /// <summary>
    /// The transport used when hosting the game over a unity relay server.
    /// </summary>
    public UnityTransport UnityRelayTransport => m_UnityRelayTransport;
}
