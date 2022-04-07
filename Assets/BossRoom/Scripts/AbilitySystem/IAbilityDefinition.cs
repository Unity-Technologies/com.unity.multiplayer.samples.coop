using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public interface IAbilityDefinition
{
    public ushort AbilityId { get; set; }

    void OnStart(AbilityInstance abilityInstance);

    void OnEnd(AbilityInstance abilityInstance);

    // public TInstanceData Create();
}

public struct AbilityInstance : INetworkSerializable, IEquatable<AbilityInstance>
{
    public float StartTime;
    public byte InstanceId;
    public bool IsNetworked;
    public ActivationContext Context;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref StartTime);
        serializer.SerializeValue(ref  InstanceId);
        Context.NetworkSerialize(serializer);

        if (serializer.IsReader)
        {
            IsNetworked = true; // Deserialized context always belongs to a networked ability
        }
    }

    public bool Equals(AbilityInstance other)
    {
        return InstanceId == other.InstanceId;
    }

    public override bool Equals(object obj)
    {
        return obj is AbilityInstance other && Equals(other);
    }

    public override int GetHashCode()
    {
        return InstanceId.GetHashCode();
    }
}
