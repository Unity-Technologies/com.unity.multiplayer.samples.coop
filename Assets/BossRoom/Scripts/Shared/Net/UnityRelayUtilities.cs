using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Relay;
using Unity.Services.Relay.Allocations;
using Unity.Services.Relay.Models;

namespace BossRoom
{
    public static class RelayJoinCodeThing
    {
        public static string RelayJoinCode = String.Empty;
    }
    public class RelayUtility
    {
        async public static
            Task<(string ipv4address, ushort port, byte[] allocationIdBytes, byte[] connectionData, byte[] key, string
                joinCode)> AllocateRelayServerAndGetJoinCode(int maxConnections, string region = null)
        {
            Allocation allocation;
            string joinCode;

            try
            {
                allocation = await Relay.Instance.CreateAllocationAsync(maxConnections, region);

            }
            catch
            {
                Debug.LogError("Relay create allocation request failed");
                throw;
            }

            Debug.Log($"server: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
            Debug.Log($"server: {allocation.AllocationId}");

            try
            {
                joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);
            }
            catch
            {
                Debug.LogError("Relay create join code request failed");
                throw;
            }

            return (allocation.RelayServer.IpV4, (ushort) allocation.RelayServer.Port, allocation.AllocationIdBytes,
                allocation.ConnectionData, allocation.Key, joinCode);
        }

        async public static
            Task<(string ipv4address, ushort port, byte[] allocationIdBytes, byte[] connectionData, byte[]
                hostConnectionData, byte[] key)> JoinRelayServerFromJoinCode(string joinCode)
        {
            JoinAllocation allocation;
            try
            {
                allocation = await Relay.Instance.JoinAllocationAsync(joinCode);
            }
            catch
            {
                Debug.LogError("Relay create join code request failed");
                throw;
            }


            Debug.Log($"client: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
            Debug.Log($"host: {allocation.HostConnectionData[0]} {allocation.HostConnectionData[1]}");
            Debug.Log($"client: {allocation.AllocationId}");

            return (allocation.RelayServer.IpV4, (ushort) allocation.RelayServer.Port, allocation.AllocationIdBytes,
                allocation.ConnectionData, allocation.HostConnectionData, allocation.Key);
        }
    }
}
