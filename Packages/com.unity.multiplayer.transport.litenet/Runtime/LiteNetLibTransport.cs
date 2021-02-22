using LiteNetLib;
using MLAPI.Transports;
using MLAPI.Transports.Tasks;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using MLAPI;
using MLAPI.Logging;
using UnityEngine;
using UnityEngine.Assertions;

namespace LiteNetLibTransport
{
    public class LiteNetLibTransport : Transport, INetEventListener
    {
        enum HostType
        {
            None,
            Server,
            Client
        }

        struct LiteChannel
        {
            public byte ChannelNumber;
            public DeliveryMethod Method;
        }

        [Tooltip("The port to listen on (if server) or connect to (if client)")]
        public ushort Port = 7777;
        [Tooltip("The address to connect to as client; ignored if server")]
        public string Address = "127.0.0.1";
        [Tooltip("Interval between ping packets used for detecting latency and checking connection, in seconds")]
        public float PingInterval = 1f;
        [Tooltip("Maximum duration for a connection to survive without receiving packets, in seconds")]
        public float DisconnectTimeout = 5f;
        [Tooltip("Delay between connection attempts, in seconds")]
        public float ReconnectDelay = 0.5f;
        [Tooltip("Maximum connection attempts before client stops and reports a disconnection")]
        public int MaxConnectAttempts = 10;
        public TransportChannel[] channels = new TransportChannel[0];
        [Tooltip("Size of default buffer for decoding incoming packets, in bytes")]
        public int MessageBufferSize = 1024 * 5;
        [Tooltip("Simulated chance for a packet to be \"lost\", from 0 (no simulation) to 100 percent")]
        public int SimulatePacketLossChance = 0;
        [Tooltip("Simulated minimum additional latency for packets in milliseconds (0 for no simulation)")]
        public int SimulateMinLatency = 0;
        [Tooltip("Simulated maximum additional latency for packets in milliseconds (0 for no simulation")]
        public int SimulateMaxLatency = 0;

        private readonly Dictionary<Channel, LiteChannel> m_LiteChannels = new Dictionary<Channel, LiteChannel>();
        readonly Dictionary<ulong, NetPeer> m_Peers = new Dictionary<ulong, NetPeer>();

        NetManager m_NetManager;

        byte[] m_MessageBuffer;

        public override ulong ServerClientId => 0;
        HostType m_HostType;

        SocketTask m_ConnectTask;

        void OnValidate()
        {
            PingInterval = Math.Max(0, PingInterval);
            DisconnectTimeout = Math.Max(0, DisconnectTimeout);
            ReconnectDelay = Math.Max(0, ReconnectDelay);
            MaxConnectAttempts = Math.Max(0, MaxConnectAttempts);
            MessageBufferSize = Math.Max(0, MessageBufferSize);
            SimulatePacketLossChance = Math.Min(100, Math.Max(0, SimulatePacketLossChance));
            SimulateMinLatency = Math.Max(0, SimulateMinLatency);
            SimulateMaxLatency = Math.Max(SimulateMinLatency, SimulateMaxLatency);
        }

        void Update()
        {
            m_NetManager?.PollEvents();
        }

        public override bool IsSupported => Application.platform != RuntimePlatform.WebGLPlayer;

        public override void Send(ulong clientId, ArraySegment<byte> data, Channel channel)
        {
            if (m_Peers.ContainsKey(clientId))
            {
                AppendChannel(ref data, channel);
                if (m_LiteChannels.TryGetValue(channel, out LiteChannel liteChannel))
                {
                    m_Peers[clientId].Send(data.Array, data.Offset, data.Count, (byte)channel, liteChannel.Method);
                }
            }
        }

        private void AppendChannel(ref ArraySegment<byte> data, Channel channel)
        {
            Assert.IsNotNull(data.Array);

            var index = data.Offset + data.Count;
            var size = index + 1;
            var array = data.Array;

            if (data.Array.Length < size)
            {
                if (size > m_MessageBuffer.Length)
                {
                    ResizeMessageBuffer(size);
                }

                array = m_MessageBuffer;
            }

            array[index] = (byte)channel;

            data = new ArraySegment<byte>(array, data.Offset, data.Count + 1);
        }

        public override NetEventType PollEvent(out ulong clientId, out Channel channel, out ArraySegment<byte> payload, out float receiveTime)
        {
            // transport is event based ignore this.
            clientId = 0;
            channel = Channel.ChannelUnused;
            receiveTime = Time.realtimeSinceStartup;
            return NetEventType.Nothing;
        }

        public override SocketTasks StartClient()
        {
            SocketTask task = SocketTask.Working;

            if (m_HostType != HostType.None)
            {
                throw new InvalidOperationException("Already started as " + m_HostType);
            }

            m_HostType = HostType.Client;

            m_NetManager.Start();

            NetPeer peer = m_NetManager.Connect(Address, Port, string.Empty);

            if (peer.Id != 0)
            {
                throw new InvalidPacketException("Server peer did not have id 0: " + peer.Id);
            }

            m_Peers[(ulong)peer.Id] = peer;

            return task.AsTasks();
        }

        public override SocketTasks StartServer()
        {
            if (m_HostType != HostType.None)
            {
                throw new InvalidOperationException("Already started as " + m_HostType);
            }

            m_HostType = HostType.Server;

            bool success = m_NetManager.Start(Port);

            return new SocketTask()
            {
                IsDone = true,
                Message = null,
                SocketError = SocketError.SocketError,
                State = null,
                Success = success,
                TransportCode = -1,
                TransportException = null
            }.AsTasks();
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            if (m_Peers.ContainsKey(clientId))
            {
                m_Peers[clientId].Disconnect();
            }
        }

        public override void DisconnectLocalClient()
        {
            m_NetManager.Flush();
            m_NetManager.DisconnectAll();
            m_Peers.Clear();
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            if (!m_Peers.ContainsKey(clientId))
            {
                return 0;
            }

            return (ulong)m_Peers[clientId].Ping * 2;
        }

        public override void Shutdown()
        {
            m_NetManager.Flush();
            m_NetManager.Stop();
            m_Peers.Clear();

            m_HostType = HostType.None;
        }

        public override void Init()
        {
            m_LiteChannels.Clear();
            MapChannels(MLAPI_CHANNELS);
            MapChannels(channels);
            if (m_LiteChannels.Count > 64)
            {
                throw new InvalidOperationException("LiteNetLib supports up to 64 channels, got: " + m_LiteChannels.Count);
            }
            m_MessageBuffer = new byte[MessageBufferSize];

            m_NetManager = new NetManager(this)
            {
                PingInterval = SecondsToMilliseconds(PingInterval),
                DisconnectTimeout = SecondsToMilliseconds(DisconnectTimeout),
                ReconnectDelay = SecondsToMilliseconds(ReconnectDelay),
                MaxConnectAttempts = MaxConnectAttempts,
                SimulatePacketLoss = SimulatePacketLossChance > 0,
                SimulationPacketLossChance = SimulatePacketLossChance,
                SimulateLatency = SimulateMaxLatency > 0,
                SimulationMinLatency = SimulateMinLatency,
                SimulationMaxLatency = SimulateMaxLatency
            };
        }

        void MapChannels(TransportChannel[] channels)
        {
            byte id = (byte)m_LiteChannels.Count;

            for (int i = 0; i < channels.Length; i++)
            {
                m_LiteChannels.Add(channels[i].Id, new LiteChannel()
                {
                    ChannelNumber = id++,
                    Method = ConvertChannelType(channels[i].Type)
                });
            }
        }


        DeliveryMethod ConvertChannelType(ChannelType type)
        {
            switch (type)
            {
                case ChannelType.Unreliable:
                    {
                        return DeliveryMethod.Unreliable;
                    }
                case ChannelType.UnreliableSequenced:
                    {
                        return DeliveryMethod.Sequenced;
                    }
                case ChannelType.Reliable:
                    {
                        return DeliveryMethod.ReliableUnordered;
                    }
                case ChannelType.ReliableSequenced:
                    {
                        return DeliveryMethod.ReliableOrdered;
                    }
                case ChannelType.ReliableFragmentedSequenced:
                    {
                        return DeliveryMethod.ReliableOrdered;
                    }
                default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                    }
            }
        }

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            if (m_ConnectTask != null)
            {
                m_ConnectTask.Success = true;
                m_ConnectTask.IsDone = true;
                m_ConnectTask = null;
            }

            var peerId = GetMlapiClientId(peer);
            InvokeOnTransportEvent(NetEventType.Connect, peerId, Channel.DefaultMessage, default, Time.time);

            m_Peers[peerId] = peer;
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (m_ConnectTask != null)
            {
                m_ConnectTask.Success = false;
                m_ConnectTask.IsDone = true;
                m_ConnectTask = null;
            }

            var peerId = GetMlapiClientId(peer);
            InvokeOnTransportEvent(NetEventType.Disconnect, GetMlapiClientId(peer), Channel.DefaultMessage, default, Time.time);

            m_Peers.Remove(peerId);
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            // Ignore
            if (m_ConnectTask != null)
            {
                m_ConnectTask.SocketError = socketError;
                m_ConnectTask.IsDone = true;
                m_ConnectTask = null;
            }
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            int size = reader.UserDataSize;
            byte[] data = m_MessageBuffer;

            if (size > m_MessageBuffer.Length)
            {
                ResizeMessageBuffer(size);
            }

            Buffer.BlockCopy(reader.RawData, reader.UserDataOffset, data, 0, size);

            // The last byte sent is used to indicate the channel so don't include it in the payload.
            var payload = new ArraySegment<byte>(data, 0, size - 1);

            Channel channel = (Channel)data[size - 1];

            InvokeOnTransportEvent(NetEventType.Data, GetMlapiClientId(peer), channel, payload, Time.time);

            reader.Recycle();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ResizeMessageBuffer(int size)
        {
            m_MessageBuffer = new byte[size];
            if (NetworkingManager.Singleton.LogLevel == LogLevel.Developer)
            {
                NetworkLog.LogWarningServer($"LiteNetLibTransport resizing messageBuffer to size of {size}.");
            }
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            // Ignore
        }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            // Ignore
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            request.Accept();
        }

        ulong GetMlapiClientId(NetPeer peer)
        {
            ulong clientId = (ulong)peer.Id;

            if (m_HostType == HostType.Server)
            {
                clientId += 1;
            }

            return clientId;
        }

        static int SecondsToMilliseconds(float seconds)
        {
            return (int)Mathf.Ceil(seconds * 1000);
        }
    }
}
