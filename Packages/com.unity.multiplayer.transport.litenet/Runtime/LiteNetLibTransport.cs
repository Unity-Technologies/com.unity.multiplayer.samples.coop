using LiteNetLib;
using MLAPI.Transports;
using MLAPI.Transports.Tasks;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace LiteNetLibTransport
{
    public class LiteNetLibTransport : Transport, INetEventListener
    {
        private enum HostType
        {
            None,
            Server,
            Client
        }

        public struct LiteChannel
        {
            public byte number;
            public DeliveryMethod method;
        }

        public struct Event
        {
            public NetEventType type;
            public ulong clientId;
            public string channelName;
            public NetPacketReader packetReader;
            public DateTime dateTime;
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

        private readonly Dictionary<ulong, NetPeer> peers = new Dictionary<ulong, NetPeer>();
        private readonly Dictionary<string, LiteChannel> liteChannels = new Dictionary<string, LiteChannel>();

        private NetManager netManager;
        private Queue<Event> eventQueue = new Queue<Event>();

        private byte[] messageBuffer;
        private WeakReference temporaryBufferReference;

        public override ulong ServerClientId => 0;
        private HostType hostType;
        private static readonly ArraySegment<byte> emptyArraySegment = new ArraySegment<byte>();

        private SocketTask connectTask;

        private void OnValidate()
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

        private void Update()
        {
            netManager?.PollEvents();
        }

        public override bool IsSupported => Application.platform != RuntimePlatform.WebGLPlayer;

        public override void Send(ulong clientId, ArraySegment<byte> data, string channelName)
        {
            LiteChannel channel = liteChannels[channelName];

            if (peers.ContainsKey(clientId))
            {
                peers[clientId].Send(data.Array, data.Offset, data.Count, channel.method);
            }
        }

        public override NetEventType PollEvent(out ulong clientId, out string channelName, out ArraySegment<byte> payload, out float receiveTime)
        {
            payload = emptyArraySegment;
            clientId = 0;
            channelName = null;
            receiveTime = Time.realtimeSinceStartup;

            if (eventQueue.Count > 0)
            {
                Event @event = eventQueue.Dequeue();

                clientId = @event.clientId;
                channelName = @event.channelName;
                receiveTime = Time.realtimeSinceStartup - ((float)DateTime.UtcNow.Subtract(@event.dateTime).TotalSeconds);

                if (@event.packetReader != null)
                {
                    int size = @event.packetReader.UserDataSize;
                    byte[] data = messageBuffer;

                    if (size > messageBuffer.Length)
                    {
                        if (temporaryBufferReference != null && temporaryBufferReference.IsAlive && ((byte[])temporaryBufferReference.Target).Length >= size)
                        {
                            data = (byte[])temporaryBufferReference.Target;
                        }
                        else
                        {
                            data = new byte[size];
                            temporaryBufferReference = new WeakReference(data);
                        }
                    }

                    Buffer.BlockCopy(@event.packetReader.RawData, @event.packetReader.UserDataOffset, data, 0, size);

                    payload = new ArraySegment<byte>(data, 0, size);
                    @event.packetReader.Recycle();
                }

                return @event.type;
            }

            return NetEventType.Nothing;
        }

        public override SocketTasks StartClient()
        {
            SocketTask task = SocketTask.Working;

            if (hostType != HostType.None)
            {
                throw new InvalidOperationException("Already started as " + hostType);
            }

            hostType = HostType.Client;

            netManager.Start();

            NetPeer peer = netManager.Connect(Address, Port, string.Empty);

            if (peer.Id != 0)
            {
                throw new InvalidPacketException("Server peer did not have id 0: " + peer.Id);
            }

            peers[(ulong)peer.Id] = peer;

            return task.AsTasks();
        }

        public override SocketTasks StartServer()
        {
            if (hostType != HostType.None)
            {
                throw new InvalidOperationException("Already started as " + hostType);
            }

            hostType = HostType.Server;

            bool success = netManager.Start(Port);

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
            if (peers.ContainsKey(clientId))
            {
                peers[clientId].Disconnect();
            }
        }

        public override void DisconnectLocalClient()
        {
            netManager.Flush();
            netManager.DisconnectAll();
            peers.Clear();
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            if (!peers.ContainsKey(clientId))
            {
                return 0;
            }

            return (ulong)peers[clientId].Ping * 2;
        }

        public override void Shutdown()
        {
            netManager.Flush();
            netManager.Stop();
            peers.Clear();

            hostType = HostType.None;
        }

        public override void Init()
        {
            liteChannels.Clear();
            MapChannels(MLAPI_CHANNELS);
            MapChannels(channels);
            AddRpcResponseChannels();

            if (liteChannels.Count > 64)
            {
                throw new InvalidOperationException("LiteNetLib supports up to 64 channels, got: " + liteChannels.Count);
            }

            messageBuffer = new byte[MessageBufferSize];

            netManager = new NetManager(this)
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

        private void MapChannels(TransportChannel[] channels)
        {
            byte id = (byte)liteChannels.Count;

            for (int i = 0; i < channels.Length; i++)
            {
                liteChannels.Add(channels[i].Name, new LiteChannel()
                {
                    number = id++,
                    method = ConvertChannelType(channels[i].Type)
                });
            }
        }

        private void AddRpcResponseChannels()
        {
            byte id = (byte)liteChannels.Count;

            foreach (DeliveryMethod method in Enum.GetValues(typeof(DeliveryMethod)) as DeliveryMethod[])
            {
                liteChannels.Add("LITENETLIB_RESPONSE_" + method.ToString(), new LiteChannel()
                {
                    number = id++,
                    method = method
                });
            }
        }

        private DeliveryMethod ConvertChannelType(ChannelType type)
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
            if (connectTask != null)
            {
                connectTask.Success = true;
                connectTask.IsDone = true;
                connectTask = null;
            }

            Event @event = new Event()
            {
                dateTime = DateTime.UtcNow,
                type = NetEventType.Connect,
                clientId = GetMLAPIClientId(peer)
            };

            peers[@event.clientId] = peer;

            eventQueue.Enqueue(@event);
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (connectTask != null)
            {
                connectTask.Success = false;
                connectTask.IsDone = true;
                connectTask = null;
            }

            Event @event = new Event()
            {
                dateTime = DateTime.UtcNow,
                type = NetEventType.Disconnect,
                clientId = GetMLAPIClientId(peer)
            };

            peers.Remove(@event.clientId);

            eventQueue.Enqueue(@event);
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            // Ignore
            if (connectTask != null)
            {
                connectTask.SocketError = socketError;
                connectTask.IsDone = true;
                connectTask = null;
            }
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            Event @event = new Event()
            {
                dateTime = DateTime.UtcNow,
                type = NetEventType.Data,
                clientId = GetMLAPIClientId(peer),
                packetReader = reader,
                channelName = "LITENETLIB_RESPONSE_" + deliveryMethod.ToString()
            };

            eventQueue.Enqueue(@event);
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

        private ulong GetMLAPIClientId(NetPeer peer)
        {
            ulong clientId = (ulong)peer.Id;

            if (hostType == HostType.Server)
            {
                clientId += 1;
            }

            return clientId;
        }

        private static int SecondsToMilliseconds(float seconds)
        {
            return (int)Mathf.Ceil(seconds * 1000);
        }
    }
}