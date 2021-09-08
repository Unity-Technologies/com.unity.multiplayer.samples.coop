using MLAPI;
using UnityEngine;

public class ServerNetworkObjectSpawner : NetworkBehaviour
{
    [SerializeField]
    NetworkObject m_NetworkObjectPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        var networkObject = Instantiate(m_NetworkObjectPrefab, transform.position, transform.rotation);

        networkObject.Spawn(destroyWithScene:true);
    }
}
