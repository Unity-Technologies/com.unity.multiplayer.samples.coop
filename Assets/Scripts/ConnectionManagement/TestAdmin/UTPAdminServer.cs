using System;
using System.Collections.Generic;
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
        while ((c = driver.Accept()) != default)
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
    public int maxMessageCountPerConnection;

    [ReadOnly] public NativeArray<MessageToSend> messagesToSendForAllConnections;
    [ReadOnly] public NativeArray<int> messageCountPerConnection;

    unsafe public void Execute(int connectionIndex)
    {
        var connection = connections[connectionIndex];
        Assert.IsTrue(connection.IsCreated);

        NativeList<MessageToSend> messagesToSendAtTheEnd = new NativeList<MessageToSend>(maxMessageCountPerConnection, Allocator.Temp);

        if (maxMessageCountPerConnection > 0)
        {
            NativeArray<MessageToSend> messagesToSendForThisConnection = new NativeArray<MessageToSend>(messageCountPerConnection[connectionIndex], Allocator.Temp);
            NativeArray<MessageToSend>.Copy(messagesToSendForAllConnections, connectionIndex * maxMessageCountPerConnection, messagesToSendForThisConnection, 0, messageCountPerConnection[connectionIndex]); // fake 2D array with offsets
            messagesToSendAtTheEnd.AddRange(messagesToSendForThisConnection);
        }

        // READ
        NetworkEvent.Type cmd;
        while ((cmd = driver.PopEventForConnection(connection, out var stream)) != NetworkEvent.Type.Empty)
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

                                messagesToSendAtTheEnd.Add(new MessageToSend() {connectionIndexDestination = connectionIndex, value = number, CommandType = MessageToSend.Command.Handshake});

                                break;
                            case MessageToSend.Command.GetPlayerCount:
                                int count = -1;
                                if (NetworkManager.Singleton.IsListening)
                                {
                                    count = NetworkManager.Singleton.ConnectedClients.Count;
                                }
                                messagesToSendAtTheEnd.Add(new MessageToSend()
                                {
                                    connectionIndexDestination = connectionIndex,
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
                    connection = default(NetworkConnection);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        // WRITE

        SendAndConsumeMessagesInList(ref messagesToSendAtTheEnd, ref driver, ref connection);
        // if (tmpSend.Length > 0)
        // {
        //     var tmpArray = tmpSend.AsArray(); // :( perf
        //     SendMessagesInList(ref tmpArray, ref driver, ref connections);
        // }
    }

    static unsafe void SendAndConsumeMessagesInList(ref NativeList<MessageToSend> messagesToSend, ref NetworkDriver.Concurrent driver, ref NetworkConnection connection)
    {
        if (messagesToSend.Length == 0) return;
        foreach (MessageToSend messageToSend in messagesToSend)
        {
            driver.BeginSend(connection, out var writer);
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
    public List<List<MessageToSend>> m_MessagesToSend;

    void Start()
    {
        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
        m_MessagesToSend = new List<List<MessageToSend>>();
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
        // Make sure we run our jobs to completion before exiting.
        ServerJobHandle.Complete();
        if (m_Connections.IsCreated) m_Connections.Dispose();
        m_Driver.Dispose();
    }

    [ContextMenu("SendToFirstClient")]
    void DoSendMessage()
    {
        m_MessagesToSend[0].Add(new MessageToSend()
        {
            CommandType = MessageToSend.Command.GetPlayerCountResponse,
            value = 123,
            connectionIndexDestination = 0
        }); // TODO have client ID system? Use connection ID?
    }

    private ServerUpdateConnectionsJob connectionJob;

    void ResetMessagesToSend()
    {
        foreach (var messageList in m_MessagesToSend)
        {
            messageList.Clear();
        }
    }
    void Update()
    {
        ServerJobHandle.Complete();
        while (connectionJob.connections.IsCreated && connectionJob.connections.Length > m_MessagesToSend.Count)
        {
            m_MessagesToSend.Add(new List<MessageToSend>());
        }

        connectionJob = new ServerUpdateConnectionsJob
        {
            driver = m_Driver,
            connections = m_Connections
        };
        // NativeArray<NativeArray<MessageToSend>> toSend = new NativeArray<NativeArray<MessageToSend>>(m_MessagesToSend.Count, Allocator.TempJob);
        // for (int i = 0; i < m_MessagesToSend.Count; i++)
        // {
        //     var messages = m_MessagesToSend[i];
        //     toSend[i] = new NativeArray<MessageToSend>(messages.ToArray(), Allocator.TempJob);
        // }


        var concurrentDriver = m_Driver.ToConcurrent();

        ServerJobHandle = m_Driver.ScheduleUpdate();
        ServerJobHandle = connectionJob.Schedule(ServerJobHandle);

        // find max amount of messages to send
        var maxCount = 0;
        foreach (var messageList in m_MessagesToSend)
        {
            if (messageList.Count > maxCount) maxCount = messageList.Count;
        }

        NativeArray<int> messageCountPerConnection = new NativeArray<int>(m_MessagesToSend.Count, Allocator.TempJob);
        NativeArray<MessageToSend> messageToSends = new NativeArray<MessageToSend>(maxCount * m_MessagesToSend.Count, Allocator.TempJob);
        for (int connectionOffset = 0; connectionOffset < m_MessagesToSend.Count; connectionOffset++) // TODO doing this here with an outdated list of connections risks sending messages to the wrong connection. We need to use the network ID instead of the index
        {
            messageCountPerConnection[connectionOffset] = m_MessagesToSend[connectionOffset].Count;
            for (int messageIndex = 0; messageIndex < m_MessagesToSend[connectionOffset].Count; messageIndex++)
            {
                messageToSends[connectionOffset * maxCount + messageIndex] = m_MessagesToSend[connectionOffset][messageIndex];
            }
        }

        // NativeArray<JobHandle> updateDependencies = new NativeArray<JobHandle>(m_Connections.Length, Allocator.Temp);
        // for (int i = 0; i < m_Connections.Length; i++)
        // {
        var serverUpdateJob = new ServerUpdateJob
        {
            driver = concurrentDriver,
            connections = m_Connections.AsDeferredJobArray(), // deferred so this job runs on an updated connection list once the dependency has run
            maxMessageCountPerConnection = maxCount,
            messagesToSendForAllConnections = messageToSends,
            messageCountPerConnection = messageCountPerConnection,
            // messagesToSend = new NativeArray<MessageToSend>(m_MessagesToSend[i].ToArray(), Allocator.TempJob),
        };
            // updateDependencies[i] = serverUpdateJob.Schedule(ServerJobHandle);
        // }
        ServerJobHandle = serverUpdateJob.Schedule(m_Connections, 1, ServerJobHandle);

        // dispose when job is done
        ServerJobHandle = messageToSends.Dispose(ServerJobHandle);
        ServerJobHandle = messageCountPerConnection.Dispose(ServerJobHandle);
        ResetMessagesToSend();

        // ServerJobHandle = JobHandle.CombineDependencies(updateDependencies);
    }
}
