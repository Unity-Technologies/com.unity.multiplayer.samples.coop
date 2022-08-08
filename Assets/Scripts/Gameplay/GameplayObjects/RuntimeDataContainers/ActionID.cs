using System;
using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public struct ActionID : INetworkSerializable, IEquatable<ActionID>
    {
        public int ID;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ID);
        }

        public bool Equals(ActionID other)
        {
            return ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            return obj is ActionID other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ID;
        }

        public static bool operator ==(ActionID x, ActionID y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(ActionID x, ActionID y)
        {
            return !(x == y);
        }
    }
}
