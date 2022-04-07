using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

static class ContextPool
{
    public const int k_DefaultSize = 128;

    static Stack<FastBufferWriter> m_Writers = new Stack<FastBufferWriter>();

    static Stack<FastBufferReader> m_Readers = new Stack<FastBufferReader>();

    static ContextPool()
    {
        m_Readers = new Stack<FastBufferReader>();
    }

    public static FastBufferWriter GetWriter()
    {
        if (m_Writers.Count > 0)
        {
            return m_Writers.Pop();
        }

        return CreateWriter();
    }

    private static FastBufferWriter CreateWriter()
    {

        return new FastBufferWriter(k_DefaultSize, Allocator.Persistent, short.MaxValue);
    }

    public static FastBufferReader GetReader(int size)
    {
        FastBufferReader reader = default;
        if (m_Readers.Count > 0)
        {
            reader = m_Readers.Pop();
        }
        else
        {
            reader = CreateReader(k_DefaultSize);
        }

        if (size > reader.Length)
        {
            reader.Dispose();
            reader = CreateReader(size);
        }

        return reader;
    }

    private static FastBufferReader CreateReader(int size)
    {
        NativeArray<byte> buffer = new NativeArray<byte>(size, Allocator.Persistent);
        return new FastBufferReader(buffer, Allocator.Temp);
    }

    public static void ReturnReader(FastBufferReader reader)
    {
        reader.Seek(0);
        m_Readers.Push(reader);
    }

    public static void ReturnWriter(FastBufferWriter writer)
    {
        writer.Seek(0);
        m_Writers.Push(writer);
    }
}

public struct SampleContextData
{
    public Vector3 AbilityDirection;
    public NetworkObjectReference AbilityTarget;

    //......
}

public struct ActivationContext : INetworkSerializable, IDisposable
{
    int m_ReaderLenght;

    public ushort AbilityId;

    FastBufferWriter m_Writer;
    FastBufferReader m_Reader;

    public bool IsCreated => m_Writer.Equals(default) == false || m_Reader.Equals(default) == false;

    internal ActivationContext(IAbilityDefinition abilityDefinition)
    {
        AbilityId = abilityDefinition.AbilityId;
        m_Writer = ContextPool.GetWriter();
        m_Writer.WriteValueSafe(AbilityId);
        m_ReaderLenght = -1;
        m_Reader = default;
    }

    public void SetNetworkSerializableData<T>(T data, int position = 0) where T : INetworkSerializable, new()
    {
        m_Writer.Seek(position + 2);
        m_Writer.WriteNetworkSerializable(data);
    }

    public void SetValueData<T>(T data, int position = 0) where T : unmanaged
    {
        m_Writer.Seek(position + 2);
        m_Writer.WriteValueSafe(data);
    }

    public T GetNetworkSerializableData<T>(int position = 0) where T : INetworkSerializable, new()
    {
        m_Reader.Seek(position + 2);
        m_Reader.ReadNetworkSerializable(out T value);
        return value;
    }

    public T GetValueData<T>(int position = 0) where T : unmanaged
    {
        m_Writer.Seek(position + 2);
        m_Reader.ReadValueSafe(out T value);
        return value;
    }

    internal unsafe void InitializeWriterFromReader()
    {
        Assert.IsTrue(m_ReaderLenght >= 0);
        m_Writer = ContextPool.GetWriter();

        UnsafeUtility.MemCpy(m_Writer.GetUnsafePtr(), m_Reader.GetUnsafePtr(), m_ReaderLenght);
        m_Reader.Seek(2);
    }

    public unsafe void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteBytesSafe(m_Writer.GetUnsafePtr(), m_Writer.Position);
        }
        else
        {
            var reader = serializer.GetFastBufferReader();
            m_Reader = ContextPool.GetReader(reader.Length);
            UnsafeUtility.MemCpy(m_Reader.GetUnsafePtr(), reader.GetUnsafePtr(), reader.Length);
            reader.ReadValueSafe(out AbilityId);
            m_ReaderLenght = reader.Length;

            //reader.ReadBytes(m_Reader.GetUnsafePtr(), reader.Length - reader.Position);
        }
    }

    public void Dispose()
    {
        ContextPool.ReturnReader(m_Reader);
        ContextPool.ReturnWriter(m_Writer);
    }
}
