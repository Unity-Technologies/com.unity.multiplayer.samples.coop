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

        // READ
        while ((cmd = connection[0].PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)
        {
            switch (cmd)
            {
                case NetworkEvent.Type.Connect:
                {
                    Debug.Log("We are now connected to the server");

                    messagesToSend.Add(new MessageToSend(){connectionIndexDestination = 0, value = 1, CommandType = MessageToSend.Command.Handshake});
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
                                Debug.Log("Got the value = " + messageReceived.value + " back from the server");
                                done[0] = 1;
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
                    return;
            }
        }

        SendAndConsumeMessagesInList(ref messagesToSend, ref driver, ref connection);
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
        Handshake,
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

public interface IUTPStateMachine // todo should be idisposable?
{
    IUTPStateMachine Initialize(UTPWrapper wrapper);
    IUTPStateMachine StartConnection();
    IUTPStateMachine Update();
    IUTPStateMachine Destroy();
    IUTPStateMachine Disconnect();
    void SendMessage(ref MessageToSend messageToSend);
}
public unsafe class Offline : IUTPStateMachine
{
    public UTPWrapper m_UtpWrapper;

    public IUTPStateMachine Initialize(UTPWrapper wrapper)
    {
        m_UtpWrapper = wrapper;
        return this;
    }

    public IUTPStateMachine StartConnection()
    {
        m_UtpWrapper.m_Driver = NetworkDriver.Create();

        m_UtpWrapper.m_Connection = new NativeArray<NetworkConnection>(1, Allocator.Persistent);
        m_UtpWrapper.m_MessagesToSend = new NativeList<MessageToSend>(Allocator.Persistent);
        m_UtpWrapper.m_Done = new NativeArray<byte>(1, Allocator.Persistent);
        var endpoint = NetworkEndPoint.LoopbackIpv4;
        endpoint.Port = 9000;
        m_UtpWrapper.m_Connection[0] = m_UtpWrapper.m_Driver.Connect(endpoint);

        m_UtpWrapper.m_CurrentState = new ClientConnecting().Initialize(m_UtpWrapper);
        return this;
    }

    public IUTPStateMachine Update()
    {
        return this;
    }

    public IUTPStateMachine Destroy()
    {
        m_UtpWrapper.LatestJobHandle.Complete();
        if (m_UtpWrapper.m_Connection.IsCreated) m_UtpWrapper.m_Connection.Dispose();
        if (m_UtpWrapper.m_MessagesToSend.IsCreated) m_UtpWrapper.m_MessagesToSend.Dispose();
        m_UtpWrapper.m_Driver.Dispose();
        if (m_UtpWrapper.m_Done.IsCreated) m_UtpWrapper.m_Done.Dispose();
        return this;
    }

    public IUTPStateMachine Disconnect()
    {
        throw new NotImplementedException();
    }

    public void SendMessage(ref MessageToSend messageToSend)
    {
        throw new NotImplementedException();
    }
}

public class ClientConnecting : IUTPStateMachine
{
    // Monitors for handshake and connection

    public UTPWrapper m_UtpWrapper;
    private ClientUpdateJob m_CurrentJob;

    public IUTPStateMachine Initialize(UTPWrapper wrapper)
    {
        m_UtpWrapper = wrapper;
        return this;
    }

    public IUTPStateMachine StartConnection()
    {
        throw new NotImplementedException();
    }

    public IUTPStateMachine Update()
    {
        m_UtpWrapper.LatestJobHandle.Complete();
        if (m_CurrentJob.done.Length > 0 && m_CurrentJob.done[0] == 1)
        {
            // we're connected, finishing this update with the next state's update
            m_UtpWrapper.m_CurrentState = new ClientConnected().Initialize(m_UtpWrapper).Update();
        }
        else
        {
            m_CurrentJob = new ClientUpdateJob()
            {
                driver = m_UtpWrapper.m_Driver,
                connection = m_UtpWrapper.m_Connection,
                done = m_UtpWrapper.m_Done,
                messagesToSend = m_UtpWrapper.m_MessagesToSend
            };
            m_UtpWrapper.LatestJobHandle = m_UtpWrapper.m_Driver.ScheduleUpdate();
            m_UtpWrapper.LatestJobHandle = m_CurrentJob.Schedule(m_UtpWrapper.LatestJobHandle);
        }

        return this;
    }

    public IUTPStateMachine Destroy()
    {
        throw new NotImplementedException();
    }

    public IUTPStateMachine Disconnect()
    {
        throw new NotImplementedException();
    }

    public void SendMessage(ref MessageToSend messageToSend)
    {
        throw new NotImplementedException();
    }
}

public class ClientConnected : IUTPStateMachine
{
    public UTPWrapper m_UtpWrapper;
    private ClientUpdateJob m_CurrentJob;

    public IUTPStateMachine Initialize(UTPWrapper wrapper)
    {
        m_UtpWrapper = wrapper;
        return this;
    }

    public IUTPStateMachine StartConnection()
    {
        throw new NotImplementedException();
    }

    public IUTPStateMachine Update()
    {
        m_UtpWrapper.LatestJobHandle.Complete();

        m_CurrentJob = new ClientUpdateJob()
        {
            driver = m_UtpWrapper.m_Driver,
            connection = m_UtpWrapper.m_Connection,
            done = m_UtpWrapper.m_Done,
            messagesToSend = m_UtpWrapper.m_MessagesToSend
        };
        m_UtpWrapper.LatestJobHandle = m_UtpWrapper.m_Driver.ScheduleUpdate();
        m_UtpWrapper.LatestJobHandle = m_CurrentJob.Schedule(m_UtpWrapper.LatestJobHandle);
        return this;
    }

    public IUTPStateMachine Destroy()
    {
        return this;
    }

    public IUTPStateMachine Disconnect()
    {
        // TODO tell the server before clearing everything here
        m_UtpWrapper.m_CurrentState = new Offline().Initialize(m_UtpWrapper);
        return this;
    }

    public void SendMessage(ref MessageToSend messageToSend)
    {
        m_UtpWrapper.LatestJobHandle.Complete();
        m_UtpWrapper.m_MessagesToSend.Add(new MessageToSend() {CommandType = MessageToSend.Command.GetPlayerCount});
    }
}
public class ServerStarting : IUTPStateMachine
{
    public UTPWrapper m_UtpWrapper;

    public IUTPStateMachine Initialize(UTPWrapper wrapper)
    {
        m_UtpWrapper = wrapper;
        return this;
    }

    public IUTPStateMachine StartConnection()
    {
        throw new NotImplementedException();
    }

    public IUTPStateMachine Update()
    {
        throw new NotImplementedException();
    }

    public IUTPStateMachine Destroy()
    {
        throw new NotImplementedException();
    }

    public IUTPStateMachine Disconnect()
    {
        // TODO cleanup server
        m_UtpWrapper.m_CurrentState = new Offline().Initialize(m_UtpWrapper);
        return this;
    }

    public void SendMessage(ref MessageToSend messageToSend)
    {
        throw new NotImplementedException();
    }
}
public class ServerStarted : IUTPStateMachine
{
    public UTPWrapper m_UtpWrapper;

    public IUTPStateMachine Initialize(UTPWrapper wrapper)
    {
        m_UtpWrapper = wrapper;
        return this;
    }

    public IUTPStateMachine StartConnection()
    {
        throw new NotImplementedException();
    }

    public IUTPStateMachine Update()
    {
        throw new NotImplementedException();
    }

    public IUTPStateMachine Destroy()
    {
        throw new NotImplementedException();
    }

    public IUTPStateMachine Disconnect()
    {
        throw new NotImplementedException();
    }

    public void SendMessage(ref MessageToSend messageToSend)
    {
        throw new NotImplementedException();
    }
}

// [BurstCompile] // TODO
public class UTPWrapper
{
    public NetworkDriver m_Driver;
    public NativeArray<NetworkConnection> m_Connection;
    public NativeArray<byte> m_Done;
    public NativeList<MessageToSend> m_MessagesToSend;

    public JobHandle LatestJobHandle;

    public IUTPStateMachine m_CurrentState;

    public UTPWrapper Initialize()
    {
        m_CurrentState = new Offline().Initialize(this);
        return this;
    }
    public void StartClient()
    {
        m_CurrentState.StartConnection();
    }

    public void Destroy()
    {
        m_CurrentState.Disconnect();
        m_CurrentState.Destroy();
    }
    public void Update()
    {
        m_CurrentState.Update();
    }

    public void SendMessage(ref MessageToSend messageToSend)
    {
        m_CurrentState.SendMessage(ref messageToSend);
    }
}

[DefaultExecutionOrder(2)] // after server for local dev
public class UTPAdminClient : MonoBehaviour
{
    private UTPWrapper m_UtpWrapper;
    void Start()
    {
        m_UtpWrapper = new UTPWrapper().Initialize();
        m_UtpWrapper.StartClient();
    }

    public void OnDestroy()
    {
        m_UtpWrapper.Destroy();
    }

    [ContextMenu("SendSomethingToServer")]
    private void SendSomething()
    {
        var toSend = new MessageToSend() {CommandType = MessageToSend.Command.GetPlayerCount};
        m_UtpWrapper.SendMessage(ref toSend);
    }

    void Update()
    {
        m_UtpWrapper.Update();
    }
}
