using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using UnityEngine;

namespace PanicBuying
{
    public class GameSceneManager : MonoBehaviour
    {
        [SerializeField]
        private NetworkState networkState;

        private void Awake()
        {
            try
            {
                if (networkState.NetworkType == NetworkState.Type.None)
                {
                    Debug.LogError("Network is not initialized");
                    return;
                }

                if (networkState.NetworkType == NetworkState.Type.Host && networkState.HostAllocation != null)
                {
                    var allocation = networkState.HostAllocation;

                    NetworkManager.Singleton.GetComponent<UnityTransport>()
                        .SetHostRelayData(
                            allocation.RelayServer.IpV4,
                            (ushort)allocation.RelayServer.Port,
                            allocation.AllocationIdBytes,
                            allocation.Key,
                            allocation.ConnectionData
                        );

                    NetworkManager.Singleton.StartHost();

                    return;
                }

                if (networkState.NetworkType == NetworkState.Type.Client && networkState.JoinAllocation != null)
                {
                    var allocation = networkState.JoinAllocation;

                    NetworkManager.Singleton.GetComponent<UnityTransport>()
                        .SetClientRelayData(
                            allocation.RelayServer.IpV4,
                            (ushort)allocation.RelayServer.Port,
                            allocation.AllocationIdBytes,
                            allocation.Key,
                            allocation.ConnectionData,
                            allocation.HostConnectionData
                        );

                    return;
                }

                Debug.LogError("Network state is invalid");
            }
            catch (RelayServiceException e)
            {
                Debug.LogError(e);
            }
        }
    }
}
