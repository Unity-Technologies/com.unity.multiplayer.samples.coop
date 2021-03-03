using ExitGames.Client.Photon;
using MLAPI.Serialization.Pooled;
using MLAPI.Transports.Tasks;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.IO;
using MLAPI.Logging;
using UnityEngine;
using UnityEngine.Serialization;

namespace MLAPI.Transports
{
    [DefaultExecutionOrder(-1000)]
    public class PhotonRealtimeTransport : Transport
    {
        [Tooltip("The nickname of the player in the photon room. This value is only relevant for other photon realtime features. Leaving it empty generates a random name.")]
        [SerializeField]
        string m_NickName;

        [Header("Server Settings")]
        [Tooltip("Unique name of the room for this session.")]
        [SerializeField]
        string m_RoomName;

        [Tooltip("The maximum amount of players allowed in the room.")]
        [SerializeField]
        byte m_MaxPlayers = 16;

        [Header("Advanced Settings")]
        [Tooltip("The first byte of the range of photon event codes which this transport will reserve for unbatched messages. Should be set to a number lower then 200 to not interfere with photon internal events. Approximately 8 events will be reserved.")]
        [SerializeField]
        byte m_ChannelIdCodesStartRange = 130;

        [Tooltip("Attaches the photon support logger to the transport. Useful for debugging disconnects or other issues.")]
        [SerializeField]
        bool m_AttachSupportLogger = false;

        [Tooltip("The batching this transport should apply to MLAPI events. None only works for very simple scenes.")]
        [SerializeField]
        BatchMode m_BatchMode = BatchMode.SendAllReliable;

        [Tooltip("The maximum size of the send queue which batches MLAPI events into Photon events.")]
        [SerializeField]
        int m_SendQueueBatchSize = 4096;

        [Tooltip("The Photon event code which will be used to send batched data over MLAPI channels.")]
        [SerializeField]
        byte m_BatchedTransportEventCode = 129;

        SocketTask m_ConnectTask;
        LoadBalancingClient m_Client;

        bool m_IsHostOrServer;

        readonly Dictionary<Channel, byte> m_ChannelToId = new Dictionary<Channel, byte>();
        readonly Dictionary<byte, Channel> m_IdToChannel = new Dictionary<byte, Channel>();
        readonly Dictionary<ushort, RealtimeChannel> m_Channels = new Dictionary<ushort, RealtimeChannel>();

        /// <summary>
        /// SendQueue dictionary is used to batch events instead of sending them immediately.
        /// </summary>
        readonly Dictionary<SendTarget, SendQueue> m_SendQueue = new Dictionary<SendTarget, SendQueue>();

        /// <summary>
        /// This exists to cache raise event options when calling <see cref="RaisePhotonEvent"./> Saves us from 2 allocations per send.
        /// </summary>
        RaiseEventOptions m_CachedRaiseEventOptions = new RaiseEventOptions() { TargetActors = new int[1] };

        ///<inheritdoc/>
        public override ulong ServerClientId => GetMlapiClientId(0, true);

        ///<inheritdoc/>
        public override void Send(ulong clientId, ArraySegment<byte> data, Channel channel)
        {
            RealtimeChannel realtimeChannel = m_Channels[m_ChannelToId[channel]];

            if (m_BatchMode == BatchMode.None)
            {
                RaisePhotonEvent(clientId, realtimeChannel.SendMode.Reliability, data, realtimeChannel.Id);
                return;
            }

            SendQueue queue;
            SendTarget sendTarget = new SendTarget(clientId, realtimeChannel.SendMode.Reliability);

            if (m_BatchMode == BatchMode.SendAllReliable)
            {
                sendTarget.IsReliable = true;
            }

            if (!m_SendQueue.TryGetValue(sendTarget, out queue))
            {
                queue = new SendQueue(m_SendQueueBatchSize);
                m_SendQueue.Add(sendTarget, queue);
            }

            if (!queue.AddEvent(realtimeChannel.Id, data))
            {
                // If we are in here data exceeded remaining queue size. This should not happen under normal operation.
                if (data.Count > queue.Size)
                {
                    // If data is too large to be batched, flush it out immediately. This happens with large initial spawn packets from MLAPI.
                    Debug.LogWarning($"Sent {data.Count} bytes on channel: {channel.ToString()}. Event size exceeds sendQueueBatchSize: ({m_SendQueueBatchSize}).");
                    RaisePhotonEvent(sendTarget.ClientId, sendTarget.IsReliable, data, realtimeChannel.Id);
                }
                else
                {
                    var sendBuffer = queue.GetData();
                    RaisePhotonEvent(sendTarget.ClientId, sendTarget.IsReliable, sendBuffer, m_BatchedTransportEventCode);
                    queue.Clear();
                    queue.AddEvent(realtimeChannel.Id, data);
                }
            }
        }

        /// <summary>
        /// Flushes all send queues. (Raises photon events with data from their buffers and clears them)
        /// </summary>
        void FlushAllSendQueues()
        {
            foreach (var kvp in m_SendQueue)
            {
                if (kvp.Value.IsEmpty()) continue;

                var sendBuffer = kvp.Value.GetData();
                RaisePhotonEvent(kvp.Key.ClientId, kvp.Key.IsReliable, sendBuffer, m_BatchedTransportEventCode);
                kvp.Value.Clear();
            }
        }

        void RaisePhotonEvent(ulong clientId, bool isReliable, ArraySegment<byte> data, byte eventCode)
        {
            m_CachedRaiseEventOptions.TargetActors[0] = GetPhotonRealtimeId(clientId);
            var sendOptions = isReliable ? SendOptions.SendReliable : SendOptions.SendUnreliable;

            // This allocates because data gets boxed to object.
            m_Client.OpRaiseEvent(eventCode, data, m_CachedRaiseEventOptions, sendOptions);
        }

        /// <summary>
        /// Photon Realtime Transport is event based. Polling will always return nothing.
        /// </summary>
        public override NetEventType PollEvent(out ulong clientId, out Channel channel, out ArraySegment<byte> payload, out float receiveTime)
        {
            clientId = 0;
            channel = default;
            receiveTime = Time.realtimeSinceStartup;
            return NetEventType.Nothing;
        }

        ///<inheritdoc/>
        public override SocketTasks StartClient()
        {
            return ConnectPeer().AsTasks();
        }

        ///<inheritdoc/>
        public override SocketTasks StartServer()
        {
            var task = ConnectPeer();
            m_IsHostOrServer = true;
            return task.AsTasks();
        }

        /// <summary>
        /// Creates and connects a peer synchronously to the region master server and returns a <see cref="SocketTask"/> containing the result.
        /// </summary>
        /// <returns></returns>
        SocketTask ConnectPeer()
        {
            m_ConnectTask = SocketTask.Working;
            InitializeClient();

            var connected = m_Client.ConnectUsingSettings(PhotonAppSettings.Instance.AppSettings);

            if (!connected)
            {
                m_ConnectTask = SocketTask.Fault;
                m_ConnectTask.Message = $"Can't connect to region: {this.m_Client.CloudRegion}";
            }

            return m_ConnectTask;
        }

        ///<inheritdoc/>
        public override void DisconnectRemoteClient(ulong clientId)
        {
            m_Client.CurrentRoom.RemovePlayer(m_Client.CurrentRoom.Players[GetPhotonRealtimeId(clientId)]);
        }

        ///<inheritdoc/>
        public override void DisconnectLocalClient()
        {
            m_Client.Disconnect();
        }

        ///<inheritdoc/>
        public override ulong GetCurrentRtt(ulong clientId)
        {
            // This is only an approximate value based on the own client's rtt to the server and could cause issues, maybe use a similar approach as the Steamworks transport.
            return (ulong)(m_Client.LoadBalancingPeer.RoundTripTime * 2);
        }

        ///<inheritdoc/>
        public override void Shutdown()
        {
            if (m_Client != null)
            {
                m_Client.EventReceived -= ClientOnEventReceived;
                if (m_Client.IsConnected)
                {
                    m_Client.Disconnect();
                }
            }
        }

        ///<inheritdoc/>
        public override void Init()
        {
            for (byte i = 0; i < MLAPI_CHANNELS.Length; i++)
            {
                m_IdToChannel.Add((byte)(i + m_ChannelIdCodesStartRange), MLAPI_CHANNELS[i].Id);
                m_ChannelToId.Add(MLAPI_CHANNELS[i].Id, (byte)(i + m_ChannelIdCodesStartRange));
                m_Channels.Add((byte)(i + m_ChannelIdCodesStartRange), new RealtimeChannel()
                {
                    Id = (byte)(i + m_ChannelIdCodesStartRange),
                    SendMode = MlapiChannelTypeToSendOptions(MLAPI_CHANNELS[i].Type)
                });
            }
        }

        /// <summary>
        /// In Update before other scripts run we dispatch incoming commands.
        /// </summary>
        void Update()
        {
            if (m_Client != null)
            {
                do { } while (m_Client.LoadBalancingPeer.DispatchIncomingCommands());
            }
        }

        /// <summary>
        /// Send batched messages out in LateUpdate.
        /// </summary>
        void LateUpdate()
        {
            FlushAllSendQueues();

            if (m_Client != null)
            {
                do { } while (m_Client.LoadBalancingPeer.SendOutgoingCommands());
            }
        }

        void InitializeClient()
        {
            // This is taken from a Photon Realtime sample to get a random user name if none is provided.
            var nickName = string.IsNullOrEmpty(m_NickName) ? m_NickName : "usr" + SupportClass.ThreadSafeRandom.Next() % 99;

            if (m_Client == null)
            {
                m_Client = new LoadBalancingClient
                {
                    LocalPlayer = { NickName = nickName },
                };
            }

            // these two settings enable (almost) zero alloc sending and receiving of byte[] content
            this.m_Client.LoadBalancingPeer.ReuseEventInstance = true;
            this.m_Client.LoadBalancingPeer.UseByteArraySlicePoolForEvents = true;

            m_Client.EventReceived += ClientOnEventReceived;
            m_Client.StateChanged += ClientOnStateChanged;
        }

        void ClientOnStateChanged(ClientState lastState, ClientState currentState)
        {
            switch (currentState)
            {
                case ClientState.ConnectedToMasterServer:

                    // Once the client does connect to the master immediately redirect to its room.
                    var enterRoomParams = new EnterRoomParams()
                    {
                        RoomName = m_RoomName,
                        RoomOptions = new RoomOptions()
                        {
                            MaxPlayers = m_MaxPlayers,
                        }
                    };

                    var success = m_IsHostOrServer ? m_Client.OpCreateRoom(enterRoomParams) : m_Client.OpJoinRoom(enterRoomParams);

                    if (!success)
                    {
                        m_ConnectTask.IsDone = true;
                        m_ConnectTask.Success = false;
                        m_ConnectTask.TransportException = new InvalidOperationException("Unable to create or join room.");
                    }

                    break;

                case ClientState.Joined:
                    if (m_AttachSupportLogger)
                    {
                        var logger = gameObject.GetComponent<SupportLogger>() ?? gameObject.AddComponent<SupportLogger>();
                        logger.Client = m_Client;
                        m_Client.ConnectionCallbackTargets.Add(logger);
                    }

                    // Client connected to the room successfully, connection process is completed
                    m_ConnectTask.IsDone = true;
                    m_ConnectTask.Success = true;
                    break;
            }
        }

        void ClientOnEventReceived(EventData eventData)
        {
            var clientId = GetMlapiClientId(eventData.Sender, false);
            var localClientId = GetMlapiClientId(m_Client.LocalPlayer.ActorNumber, false);

            // Clients should ignore connection events from other clients, server should ignore its own connection event.
            var isRelevantConnectionUpdateMessage = m_IsHostOrServer ^ clientId == localClientId;

            NetEventType netEvent = NetEventType.Nothing;
            ArraySegment<byte> payload = default;
            Channel channel = default;
            var receiveTime = Time.realtimeSinceStartup;

            switch (eventData.Code)
            {
                case EventCode.Leave:
                    if (isRelevantConnectionUpdateMessage)
                    {
                        netEvent = NetEventType.Disconnect;
                    }

                    break;

                case EventCode.Join:
                    if (isRelevantConnectionUpdateMessage)
                    {
                        netEvent = NetEventType.Connect;
                    }

                    break;

                default:
                    if (eventData.Code >= 200) { return; } // EventCode is a photon event.

                    using (ByteArraySlice slice = eventData.CustomData as ByteArraySlice)
                    {
                        if (slice == null)
                        {
                            Debug.LogError("Photon option UseByteArraySlicePoolForEvents should be set to true.");
                            return;
                        }

                        if (eventData.Code == this.m_BatchedTransportEventCode)
                        {
                            using (PooledBitStream stream = PooledBitStream.Get())
                            {
                                // moving data from one pooled wrapper to another (for MLAPI to read incoming data)
                                stream.Position = 0;
                                stream.Write(slice.Buffer, slice.Offset, slice.Count);
                                stream.SetLength(slice.Count);
                                stream.Position = 0;

                                using (PooledBitReader reader = PooledBitReader.Get(stream))
                                {
                                    while (stream.Position < stream.Length)
                                    {
                                        byte channelId = reader.ReadByteDirect();
                                        int length = reader.ReadInt32Packed();
                                        byte[] dataArray = reader.ReadByteArray(null, length);

                                        this.InvokeOnTransportEvent(NetEventType.Data, clientId, this.m_IdToChannel[channelId], new ArraySegment<byte>(dataArray, 0, dataArray.Length), receiveTime);
                                    }
                                }
                            }

                            return;
                        }
                        else
                        {
                            // Event is a non-batched data event.
                            netEvent = NetEventType.Data;
                            payload = new ArraySegment<byte>(slice.Buffer, slice.Offset, slice.Count);
                            channel = m_IdToChannel[eventData.Code];
                        }
                    }

                    break;
            }

            if (netEvent == NetEventType.Nothing) return;
            InvokeOnTransportEvent(netEvent, clientId, channel, payload, receiveTime);
        }

        SendOptions MlapiChannelTypeToSendOptions(ChannelType type)
        {
            switch (type)
            {
                case ChannelType.Unreliable:
                    return SendOptions.SendUnreliable;
                default:
                    return SendOptions.SendReliable;
            }
        }

        ulong GetMlapiClientId(int photonId, bool isServer)
        {
            if (isServer)
            {
                return 0;
            }
            else
            {
                return (ulong)(photonId + 1);
            }
        }

        int GetPhotonRealtimeId(ulong clientId)
        {
            if (clientId == 0)
            {
                return m_Client.CurrentRoom.masterClientId;
            }
            else
            {
                return (int)(clientId - 1);
            }
        }

        class SendQueue
        {
            MemoryStream m_Stream;

            /// <summary>
            /// The size of the send queue.
            /// </summary>
            public int Size { get; }

            public SendQueue(int size)
            {
                Size = size;
                byte[] buffer = new byte[size];
                m_Stream = new MemoryStream(buffer, 0, buffer.Length, true, true);
            }

            /// <summary>
            /// Ads an event to the send queue.
            /// </summary>
            /// <param name="channelId">The channel this event should be sent on.</param>
            /// <param name="data">The data to send.</param>
            /// <returns>True if the event was added successfully to the queue. False if there was no space in the queue.</returns>
            internal bool AddEvent(byte channelId, ArraySegment<byte> data)
            {
                if (m_Stream.Position + data.Count + 4 > Size)
                {
                    return false;
                }

                using (PooledBitWriter writer = PooledBitWriter.Get(m_Stream))
                {
                    writer.WriteByte(channelId);
                    writer.WriteInt32Packed(data.Count);
                    Array.Copy(data.Array, data.Offset, m_Stream.GetBuffer(), m_Stream.Position, data.Count);
                    m_Stream.Position += data.Count;
                }

                return true;
            }

            internal void Clear()
            {
                m_Stream.Position = 0;
            }

            internal bool IsEmpty()
            {
                return m_Stream.Position == 0;
            }

            internal ArraySegment<byte> GetData()
            {
                return new ArraySegment<byte>(m_Stream.GetBuffer(), 0, (int)m_Stream.Position);
            }
        }

        struct RealtimeChannel
        {
            public byte Id;
            public SendOptions SendMode;
        }

        struct SendTarget: IEquatable<SendTarget>
        {
            public ulong ClientId;
            public bool IsReliable;

            public SendTarget(ulong clientId, bool isReliable)
            {
                ClientId = clientId;
                IsReliable = isReliable;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (ClientId.GetHashCode() * 397) ^ IsReliable.GetHashCode();
                }
            }

            public override bool Equals(object obj)
            {
                return obj is SendTarget other && Equals(other);
            }

            public static bool operator ==(SendTarget a, SendTarget b)
            {
                return a.ClientId == b.ClientId && a.IsReliable == b.IsReliable;
            }

            public static bool operator !=(SendTarget a, SendTarget b)
            {
                return !(a == b);
            }

            public bool Equals(SendTarget other)
            {
                return ClientId == other.ClientId && IsReliable == other.IsReliable;
            }
        }

        enum BatchMode : byte
        {
            /// <summary>
            /// The transport performs no batching.
            /// </summary>
            None = 0,
            /// <summary>
            /// Batches all MLAPI events into reliable sequenced messages.
            /// </summary>
            SendAllReliable = 1,
            /// <summary>
            /// Batches all reliable MLAPI events into a single photon event and all unreliable MLAPI events into an unreliable photon event.
            /// </summary>
            ReliableAndUnreliable = 2,
        }
    }
}
