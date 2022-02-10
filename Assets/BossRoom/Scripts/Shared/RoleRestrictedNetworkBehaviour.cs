using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// NetworkBehaviour that is only enabled when spawned, on either the server or a client.
/// </summary>
public abstract class RoleRestrictedNetworkBehaviour : NetworkBehaviour
{
    /// <summary>
    /// Returns the role for which this NetworkBehaviour should be enabled.
    /// </summary>
    /// <returns></returns>
    protected abstract Role GetTargetRole();

    protected enum Role
    {
        Client,
        Server
    }

    void Awake()
    {
        // Disable this NetworkBehaviour until it is spawned
        enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        // enable this NetworkBehaviour only for the target role
        switch (GetTargetRole())
        {
            case Role.Client:
                enabled = IsClient;
                break;
            case Role.Server:
                enabled = IsServer;
                break;
        }
    }

    public override void OnNetworkDespawn()
    {
        // Disable This NetworkBehaviour again, until it is either destroyed or spawned again
        enabled = false;
    }
}
