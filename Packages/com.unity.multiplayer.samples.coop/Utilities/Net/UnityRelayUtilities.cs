using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public static class UnityRelayUtilities
    {
        private const string kDtlsConnType = "dtls";

        public static async
            Task<(string ipv4address, ushort port, byte[] allocationIdBytes, byte[] connectionData, byte[] key, string
                joinCode)> AllocateRelayServerAndGetJoinCode(int maxConnections, string region = null)
        {
            Allocation allocation;
            string joinCode;

            try
            {
                allocation = await Relay.Instance.CreateAllocationAsync(maxConnections, region);
            }
            catch (Exception exception)
            {
                throw new Exception($"Creating allocation request has failed: \n {exception.Message}");
            }

            Debug.Log($"server: connection data: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}, allocation ID:{allocation.AllocationId}, region:{allocation.Region}");

            try
            {
                joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);
            }
            catch (Exception exception)
            {
                throw new Exception($"Creating join code request has failed: \n {exception.Message}");
            }

            var dtlsEndpoint = allocation.ServerEndpoints.First(e => e.ConnectionType == kDtlsConnType);
            return (dtlsEndpoint.Host, (ushort)dtlsEndpoint.Port, allocation.AllocationIdBytes,
                allocation.ConnectionData, allocation.Key, joinCode);
        }

        public static async
            Task<(string ipv4address, ushort port, byte[] allocationIdBytes, byte[] connectionData, byte[]
                hostConnectionData, byte[] key)> JoinRelayServerFromJoinCode(string joinCode)
        {
            JoinAllocation allocation;
            try
            {
                allocation = await Relay.Instance.JoinAllocationAsync(joinCode);
            }
            catch (Exception exception)
            {
                throw new Exception($"Creating join code request has failed: \n {exception.Message}");
            }

            Debug.Log($"client: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
            Debug.Log($"host: {allocation.HostConnectionData[0]} {allocation.HostConnectionData[1]}");
            Debug.Log($"client: {allocation.AllocationId}");

            var dtlsEndpoint = allocation.ServerEndpoints.First(e => e.ConnectionType == kDtlsConnType);
            return (dtlsEndpoint.Host, (ushort)dtlsEndpoint.Port, allocation.AllocationIdBytes,
                allocation.ConnectionData, allocation.HostConnectionData, allocation.Key);
        }
    }
}
