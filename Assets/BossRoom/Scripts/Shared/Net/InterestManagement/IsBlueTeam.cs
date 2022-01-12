using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Interest;
using UnityEngine;
using UnityEngine.Serialization;

// Nodes are for state, kernels are for computation
public class TeamInterestNode : IInterestNode<NetworkObject>
{
    private Dictionary<NetworkObject, bool> m_PlayerIsBlueTeam = new Dictionary<NetworkObject, bool>();
    private HashSet<NetworkObject> m_BlueTeamObjects = new HashSet<NetworkObject>();
    private HashSet<NetworkObject> m_RedTeamObjects = new HashSet<NetworkObject>();

    public void QueryFor(NetworkObject playerObject, HashSet<NetworkObject> results)
    {
        if (m_PlayerIsBlueTeam[playerObject])
        {
            results.UnionWith(m_BlueTeamObjects);
        }
        else
        {
            results.UnionWith(m_RedTeamObjects);
        }
    }

    public void AddObject(NetworkObject obj)
    {
        // empty
    }

    // InterestManager is internal, so I can't access this from gameplay scripts :(
    public void AddTeamObject(IsBlueTeam self)
    {
        if (self.isBlueTeam)
        {
            m_BlueTeamObjects.Add(self.NetworkObject); // requires "self" to be a NetworkBehaviour :( or to do a GetComponent<NetworkObject>
        }
        else
        {
            m_RedTeamObjects.Add(self.NetworkObject);
        }

        var selfNetworkObject = self.NetworkObject;
        if (selfNetworkObject.IsPlayerObject)
        {
            m_PlayerIsBlueTeam[selfNetworkObject] = self.isBlueTeam; // caching player team so we don't have to query it each time :)
        }
    }

    public void RemoveObject(NetworkObject obj)
    {
        throw new System.NotImplementedException();
    }

    public void UpdateObject(NetworkObject obj)
    {
        throw new System.NotImplementedException();
    }

    public void AddAdditiveKernel(IInterestKernel<NetworkObject> kernel)
    {
        throw new System.NotImplementedException();
    }

    public void AddSubtractiveKernel(IInterestKernel<NetworkObject> kernel)
    {
        throw new System.NotImplementedException();
    }
}

public class IsBlueTeam : NetworkBehaviour
{
    [SerializeField]
    public bool isBlueTeam;

    private static TeamInterestNode s_Node;

    static IsBlueTeam()
    {
        s_Node = new TeamInterestNode();
    }

    private void Awake()
    {
        var NO = NetworkObject; // ref might not be needed here (NetworkObjects are classes which are ref types). else this is needed instead of calling directly with this.NetworkObject
        NetworkManager.Singleton.InterestManager.AddInterestNode(ref NO, s_Node); // can't add node statically to interest without a NetworkObject as a second parameter?
    }

    public override void OnNetworkSpawn() // the following needs to be in OnNetworkSpawn, since I'm checking if "IsPlayerObject"
    {
        // get team node and register self to right team, without having to do GetComponent
        s_Node.AddTeamObject(this);
    }
}
