using System;
using UnityEngine;
using UnityEngine.Assertions;

using Unity.Jobs;
using Unity.Collections;
using Unity.Netcode;
using Unity.Networking.Transport;
using NetworkEvent = Unity.Networking.Transport.NetworkEvent;

struct ServerUpdateConnectionsJob : IJob
{
    public NetworkDriver driver;
    public NativeList<NetworkConnection> connections;

    public void Execute()
    {
        // CleanUpConnections
        for (int i = 0; i < connections.Length; i++)
        {
            if (!connections[i].IsCreated)
            {
                connections.RemoveAtSwapBack(i);
                --i;
            }
        }
        // AcceptNewConnections
        NetworkConnection c;
        while ((c = driver.Accept()) != default(NetworkConnection))
        {
            connections.Add(c);
            Debug.Log("Accepted a connection");
        }
    }
}

struct ServerUpdateJob : IJobParallelForDefer
{
    public NetworkDriver.Concurrent driver;
    public NativeArray<NetworkConnection> connections;
    // public NativeList<MessageToSend> messagesToSend;

    unsafe public void Execute(int index)
    {
        DataStreamReader stream;
        Assert.IsTrue(connections[index].IsCreated);

        NativeList<MessageToSend> messagesToSend = new NativeList<MessageToSend>(Allocator.Temp);

        // READ
        NetworkEvent.Type cmd;
        while ((cmd = driver.PopEventForConnection(connections[index], out stream)) != NetworkEvent.Type.Empty)
        {
            switch (cmd)
            {
                case NetworkEvent.Type.Empty:
                    break;
                case NetworkEvent.Type.Connect:
                    break;
                case NetworkEvent.Type.Data:
                    int size = 1300;
                    fixed (byte* data = new byte[size])
                    {
                        stream.ReadBytes(data, stream.Length);

                        using var tmpSerializer = new FastBufferReader(data, Allocator.Temp, size);
                        tmpSerializer.ReadValueSafe(out MessageToSend messageReceived);
                        switch (messageReceived.CommandType)
                        {
                            case MessageToSend.Command.Handshake:

                                var number = messageReceived.value;
                                Debug.Log("Got " + number + " from the Client adding + 2 to it.");
                                number += 2;

                                messagesToSend.Add(new MessageToSend() {connectionIndexDestination = index, value = number, CommandType = MessageToSend.Command.Handshake});

                                // driver.BeginSend(connections[index], out var writer);
                                // writer.WriteUInt(number);
                                // driver.EndSend(writer);
                                break;
                            case MessageToSend.Command.GetPlayerCount:
                                int count = -1;
                                if (NetworkManager.Singleton.IsListening)
                                {
                                    count = NetworkManager.Singleton.ConnectedClients.Count;
                                }
                                messagesToSend.Add(new MessageToSend()
                                {
                                    connectionIndexDestination = index,
                                    value = count,
                                    CommandType = MessageToSend.Command.GetPlayerCountResponse
                                });
                                break;
                            default:
                                Debug.Log($"unknown command type in message received {messageReceived.CommandType}");
                                break;
                        }
                    }

                    break;
                case NetworkEvent.Type.Disconnect:
                    Debug.Log("Client disconnected from server");
                    connections[index] = default(NetworkConnection);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        // WRITE

        SendAndConsumeMessagesInList(ref messagesToSend, ref driver, ref connections);
        // if (tmpSend.Length > 0)
        // {
        //     var tmpArray = tmpSend.AsArray(); // :( perf
        //     SendMessagesInList(ref tmpArray, ref driver, ref connections);
        // }
    }

    static unsafe void SendAndConsumeMessagesInList(ref NativeList<MessageToSend> messagesToSend, ref NetworkDriver.Concurrent driver, ref NativeArray<NetworkConnection> connections)
    {
        if (messagesToSend.Length == 0) return;
        foreach (MessageToSend messageToSend in messagesToSend)
        {
            driver.BeginSend(connections[messageToSend.connectionIndexDestination], out var writer);
            SerializeInWriter(ref writer, messageToSend);
            driver.EndSend(writer); // todo not great to send for each message, should structure messages by connection
        }
        messagesToSend.Clear();
    }

    public static unsafe void SerializeInWriter(ref DataStreamWriter writer, MessageToSend messageToSend)
    {
        using var tmpSerializer = new FastBufferWriter(1300, Allocator.Temp);
        tmpSerializer.WriteValueSafe(messageToSend);
        writer.WriteBytes(tmpSerializer.GetUnsafePtr(), tmpSerializer.Length);
    }
}

[DefaultExecutionOrder(1)]
public class UTPAdminServer : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NativeList<NetworkConnection> m_Connections;
    private JobHandle ServerJobHandle;
    // public NativeList<MessageToSend> m_MessagesToSend;

    void Start()
    {
        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
        // m_MessagesToSend = new NativeList<MessageToSend>(Allocator.Persistent);
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = 9000;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port 9000");
        else
            m_Driver.Listen();
    }

    public void OnDestroy()
    {
        ServerJobHandle.Complete();
        // Make sure we run our jobs to completion before exiting.
        // if (m_Driver.IsCreated)
        {
            ServerJobHandle.Complete();
            m_Connections.Dispose();
            // m_MessagesToSend.Dispose();
            m_Driver.Dispose();
        }
    }

    void Update()
    {
        ServerJobHandle.Complete();

        var connectionJob = new ServerUpdateConnectionsJob
        {
            driver = m_Driver,
            connections = m_Connections
        };

        var serverUpdateJob = new ServerUpdateJob
        {
            driver = m_Driver.ToConcurrent(),
            connections = m_Connections.AsDeferredJobArray(),
            // messagesToSend = m_MessagesToSend
        };

        ServerJobHandle = m_Driver.ScheduleUpdate();
        ServerJobHandle = connectionJob.Schedule(ServerJobHandle);
        ServerJobHandle = serverUpdateJob.Schedule(m_Connections, 1, ServerJobHandle);
    }
}
