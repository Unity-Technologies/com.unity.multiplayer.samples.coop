using System;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public struct NetworkGuid : INetworkSerializable
    {
        public ulong FirstHalf;
        public ulong SecondHalf;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref FirstHalf);
            serializer.SerializeValue(ref SecondHalf);
        }
    }

    public static class NetworkGuidExtensions
    {
        public static NetworkGuid ToNetworkGuid(this Guid id)
        {
            var networkId = new NetworkGuid();
            networkId.FirstHalf = BitConverter.ToUInt64(id.ToByteArray(), 0);
            networkId.SecondHalf = BitConverter.ToUInt64(id.ToByteArray(), 8);
            return networkId;
        }

        public static Guid ToGuid(this NetworkGuid networkId)
        {
            var bytes = new byte[16];
            Buffer.BlockCopy(BitConverter.GetBytes(networkId.FirstHalf), 0, bytes, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(networkId.SecondHalf), 0, bytes, 8, 8);
            return new Guid(bytes);
        }
    }
}
