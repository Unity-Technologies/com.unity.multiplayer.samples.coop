using System;
using System.Collections;
using System.Collections.Generic;
using BossRoom;
using MLAPI;
using MLAPI.Transports;
using MLAPI.Transports.UNET;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// This exists as a small helper to easily swap transports while still supporting the UI switch from ip to relay based transports.
/// </summary>
public class TransportPicker : MonoBehaviour
{
    [SerializeField]
    Transport m_IpHostTransport;

    [SerializeField]
    Transport m_RelayTransport;

    /// <summary>
    /// The transport used when hosting the game on an IP address.
    /// </summary>
    public Transport IpHostTransport => m_IpHostTransport;

    /// <summary>
    /// The transport used when hosting the game over a relay server.
    /// </summary>
    public Transport RelayTransport => m_RelayTransport;

    void OnValidate()
    {
        Assert.IsTrue(m_IpHostTransport == null || (m_IpHostTransport as UnetTransport || m_IpHostTransport as LiteNetLibTransport.LiteNetLibTransport),
            "IpHost transport must be either Unet or LiteNetLib transport.");

        Assert.IsTrue(m_RelayTransport == null || (m_RelayTransport as PhotonRealtimeTransport), "" +
            "Relay transport must be PhotonRealtimeTransport");
    }
}
