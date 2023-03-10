using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Netcode;
using Unity.Networking.Transport;
using NetworkEvent = Unity.Networking.Transport.NetworkEvent;

struct ClientUpdateJob : IJob
{
    public NetworkDriver driver;
    public NativeArray<NetworkConnection> connection;
    public NativeArray<byte> done;
    public NativeList<MessageToSend> messagesToSend;

    unsafe public void Execute()
    {
        if (!connection[0].IsCreated)
        {
            if (done[0] != 1)
                Debug.Log("Something went wrong during connect");
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;

        // NativeList<MessageToSend> tmpSend = new NativeList<MessageToSend>(Allocator.Temp);

        // READ
        while ((cmd = connection[0].PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)
        {
            switch (cmd)
            {
                case NetworkEvent.Type.Connect:
                {
                    Debug.Log("We are now connected to the server");

                    messagesToSend.Add(new MessageToSend(){connectionIndexDestination = 0, value = 1, CommandType = MessageToSend.Command.Handshake});

                    // uint value = 1;
                    // driver.BeginSend(connection[0], out var writer);
                    // writer.WriteUInt(value);
                    // driver.EndSend(writer);
                    break;
                }
                case NetworkEvent.Type.Data:
                {
                    int size = 1300;
                    fixed (byte* data = new byte[size])
                    {
                        stream.ReadBytes(data, stream.Length);

                        using var tmpSerializer = new FastBufferReader(data, Allocator.Temp, size);
                        tmpSerializer.ReadValueSafe(out MessageToSend messageReceived);
                        switch (messageReceived.CommandType)
                        {
                            case MessageToSend.Command.Handshake:
                                // uint value = stream.ReadUInt();
                                Debug.Log("Got the value = " + messageReceived.value + " back from the server");
                                // And finally change the `done[0]` to `1`
                                done[0] = 1;
                                // connection[0].Disconnect(driver);
                                // connection[0] = default;
                                break;
                            case MessageToSend.Command.GetPlayerCountResponse:
                                Debug.Log($"Got player count from server {messageReceived.value}");
                                break;
                            default:
                                Debug.Log($"unknown command type from server {messageReceived.CommandType}");
                                break;
                        }
                    }

                    break;
            }
                case NetworkEvent.Type.Disconnect:
                {
                    Debug.Log("Client got disconnected from server");
                    connection[0] = default;
                    break;
                }
                case NetworkEvent.Type.Empty:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        SendAndConsumeMessagesInList(ref messagesToSend, ref driver, ref connection);
        // var arrayTmp = tmpSend.AsArray();
        // SendMessagesInList(ref arrayTmp, ref driver, ref connection);
        // WRITE
        // foreach (MessageToSend messageToSend in messagesToSend)
        // {
        //     driver.BeginSend(connection[0], out var writer);
        //     using var tmpSerializer = new FastBufferWriter(1300, Allocator.Temp);
        //     tmpSerializer.WriteValueSafe(messageToSend);
        //     writer.WriteBytes(tmpSerializer.GetUnsafePtr(), tmpSerializer.Length);
        //     driver.EndSend(writer);
        // }
    }

    static void SendAndConsumeMessagesInList(ref NativeList<MessageToSend> messagesToSend, ref NetworkDriver driver, ref NativeArray<NetworkConnection> connections)
    {
        if (messagesToSend.Length == 0) return;
        driver.BeginSend(connections[0], out var writer);
        foreach (MessageToSend messageToSend in messagesToSend)
        {
            ServerUpdateJob.SerializeInWriter(ref writer, messageToSend);
        }
        driver.EndSend(writer);
        messagesToSend.Clear();
    }
}

public struct MessageToSend : INetworkSerializable
{
    public enum Command
    {
        Invalid, // shouldn't be here
        GetPlayerCount,
        GetPlayerCountResponse,
        Handshake
    }

    public Command CommandType;
    public int value;
    public int connectionIndexDestination; // not serialized
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref CommandType);
        serializer.SerializeValue(ref value);
    }
}

[DefaultExecutionOrder(2)] // after server for local dev
public class UTPAdminClient : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NativeArray<NetworkConnection> m_Connection;
    public NativeArray<byte> m_Done;
    public NativeList<MessageToSend> m_MessagesToSend;

    public JobHandle ClientJobHandle;

    void Start()
    {
        m_Driver = NetworkDriver.Create();

        m_Connection = new NativeArray<NetworkConnection>(1, Allocator.Persistent);
        m_MessagesToSend = new NativeList<MessageToSend>(Allocator.Persistent);
        m_Done = new NativeArray<byte>(1, Allocator.Persistent);
        var endpoint = NetworkEndPoint.LoopbackIpv4;
        endpoint.Port = 9000;
        m_Connection[0] = m_Driver.Connect(endpoint);
    }

    public void OnDestroy()
    {
        ClientJobHandle.Complete();
        if (m_Connection.IsCreated) m_Connection.Dispose();
        if (m_MessagesToSend.IsCreated) m_MessagesToSend.Dispose();
        m_Driver.Dispose();
        if (m_Done.IsCreated) m_Done.Dispose();
    }

    private ClientUpdateJob m_CurrentJob;

    [ContextMenu("SendSomethingToServer")]
    private void SendSomething()
    {
        ClientJobHandle.Complete();
        m_MessagesToSend.Add(new MessageToSend() {CommandType = MessageToSend.Command.GetPlayerCount});
    }

    void Update()
    {
        ClientJobHandle.Complete();
        // if (m_CurrentJob.done.Length > 0 && m_CurrentJob.done[0] == 1)
        // {
        //     Debug.Log("got an answer from the server, can do things now");
        // }

        m_CurrentJob = new ClientUpdateJob
        {
            driver = m_Driver,
            connection = m_Connection,
            done = m_Done,
            messagesToSend = m_MessagesToSend
        };
        ClientJobHandle = m_Driver.ScheduleUpdate();
        ClientJobHandle = m_CurrentJob.Schedule(ClientJobHandle);
    }
}
