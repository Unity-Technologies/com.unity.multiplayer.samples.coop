using ExitGames.Client.Photon;
using MLAPI.Serialization.Pooled;
using MLAPI.Transports.Tasks;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.IO;
using MLAPI.Logging;
using UnityEngine;

namespace MLAPI.Transports
{
    [DefaultExecutionOrder(-1000)]
    public class PhotonRealtimeTransport : Transport
    {
        [Header("Photon Cloud Settings")] [SerializeField]
        private string appId;

        [SerializeField] private string gameVersion = "0.0.0";

        [Tooltip("The region master server to connect to. See https://doc.photonengine.com/en-us/realtime/current/connection-and-authentication/regions#available_regions for a list of regions.")]
        [SerializeField]
        private string region = "EU";

        [Tooltip("The nickname of the player in the photon room. This value is only relevant for other photon realtime features. Leaving it empty generates a random name.")]
        [SerializeField]
        private string nickName;

        [Header("Server Settings")] [Tooltip("Unique name of the room for this session.")] [SerializeField]
        private string roomName;

        [Tooltip("The maximum amount of players allowed in the room.")] [SerializeField]
        private byte maxPlayers = 16;

        [Header("Advanced Settings")]
        [Tooltip("The Photon event code which will be used to send data over MLAPI channels.")]
        [SerializeField]
        private byte batchedTransportEventCode = 129;

        [Tooltip("The first byte of the range of photon event codes which this transport will reserve for unbatched messages. Should be set to a number lower then 128 to not interfere with photon internal events. Approximately 8 events will be reserved.")]
        [SerializeField]
        private byte channelIdCodesStartRange = 130;

        [Tooltip("Attaches the photon support logger to the transport. Useful for debugging disconnects or other issues.")]
        [SerializeField]
        private bool attachSupportLogger = false;

        [Tooltip("The maximum size of the send queue which batches MLAPI events into Photon events.")]
        [SerializeField]
        private int sendQueueBatchSize = 4096;

        private SocketTask connectTask;

        private LoadBalancingClient client;

        private bool isHostOrServer;

        private readonly Dictionary<string, byte> channelNameToId = new Dictionary<string, byte>();
        private readonly Dictionary<byte, string> channelIdToName = new Dictionary<byte, string>();
        private readonly Dictionary<ushort, RealtimeChannel> channels = new Dictionary<ushort, RealtimeChannel>();

        private readonly Dictionary<ulong, SendQueue> sendQueue = new Dictionary<ulong, SendQueue>();

        ///<inheritdoc/>
        public override ulong ServerClientId => GetMLAPIClientId(0, true);

        ///<inheritdoc/>
        public override void Send(ulong clientId, ArraySegment<byte> data, string channelName)
        {
            RealtimeChannel channel = channels[channelNameToId[channelName]];

            SendQueue queue;
            if (!sendQueue.TryGetValue(clientId, out queue))
            {
                queue = new SendQueue(sendQueueBatchSize);
                sendQueue.Add(clientId, queue);
            }

            if (!queue.AddEvent(channel.Id, data))
            {
                if (data.Count > queue.Size)
                {
                    Debug.LogWarning($"Sent {data.Count} bytes on channel: {channelName}. Event size exceeds sendQueueBatchSize: ({sendQueueBatchSize}).");
                    RaisePhotonEvent(clientId, data, channel.Id);
                }
                else
                {
                    var sendBuffer = queue.GetData();
                    RaisePhotonEvent(clientId, sendBuffer, batchedTransportEventCode);
                    queue.Clear();
                    queue.AddEvent(channel.Id, data);
                }
            }
        }

        private void FlushAllSendQueues()
        {
            foreach (var kvp in sendQueue)
            {
                if (kvp.Value.IsEmpty())continue;

                var sendBuffer = kvp.Value.GetData();
                RaisePhotonEvent(kvp.Key, sendBuffer, batchedTransportEventCode);
                kvp.Value.Clear();
            }
        }

        private void RaisePhotonEvent(ulong clientId, ArraySegment<byte> data, byte eventCode)
        {
            client.OpRaiseEvent(
                eventCode,
                data,
                new RaiseEventOptions()
                {
                    TargetActors = new int[] { GetPhotonRealtimeId(clientId) }
                },
                SendOptions.SendReliable);
        }

        ///<inheritdoc/>
        public override NetEventType PollEvent(out ulong clientId, out string channelName, out ArraySegment<byte> payload, out float receiveTime)
        {
            clientId = 0;
            channelName = null;
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
            isHostOrServer = true;
            return task.AsTasks();
        }

        private SocketTask ConnectPeer()
        {
            connectTask = SocketTask.Working;
            InitializeClient();

            bool couldConnect = client.ConnectToRegionMaster(region);

            if (!couldConnect)
            {
                connectTask = SocketTask.Fault;
                connectTask.Message = $"Can't connect to region: {region}";
            }
            return connectTask;
        }

        ///<inheritdoc/>
        public override void DisconnectRemoteClient(ulong clientId)
        {
            client.CurrentRoom.RemovePlayer(client.CurrentRoom.Players[GetPhotonRealtimeId(clientId)]);
        }

        ///<inheritdoc/>
        public override void DisconnectLocalClient()
        {
            client.Disconnect();
        }

        ///<inheritdoc/>
        public override ulong GetCurrentRtt(ulong clientId)
        {
            // This is only an approximate value based on the own client's rtt to the server and could cause issues, maybe use a similar approach as the Steamworks transport.
            return (ulong) (client.LoadBalancingPeer.RoundTripTime * 2);
        }

        ///<inheritdoc/>
        public override void Shutdown()
        {

            if (client != null)
            {
                client.EventReceived -= ClientOnEventReceived;
                if (client.IsConnected)
                {
                    client.Disconnect();
                }
            }
        }

        ///<inheritdoc/>
        public override void Init()
        {
            for (byte i = 0; i < MLAPI_CHANNELS.Length; i++)
            {
                channelIdToName.Add((byte) (i + channelIdCodesStartRange), MLAPI_CHANNELS[i].Name);
                channelNameToId.Add(MLAPI_CHANNELS[i].Name, (byte)(i + channelIdCodesStartRange));
                channels.Add((byte)(i + channelIdCodesStartRange), new RealtimeChannel()
                {
                    Id = (byte)(i + channelIdCodesStartRange),
                    Name = MLAPI_CHANNELS[i].Name,
                    SendMode = MLAPIChannelTypeToSendOptions(MLAPI_CHANNELS[i].Type)
                });
            }
        }

        private void Update()
        {
            if (client != null)
            {
                do ;
                while (client.LoadBalancingPeer.DispatchIncomingCommands());
            }
        }

        private void LateUpdate()
        {
            // Send messages at least once per update to make sure to receive messages even if MLAPI is not polling.
            FlushAllSendQueues();

            if (client != null)
            {
                do ;
                while (client.LoadBalancingPeer.SendOutgoingCommands());
            }
        }

        private void InitializeClient()
        {
            if (client == null)
            {
                client = new LoadBalancingClient()
                {
                    AppId = appId,
                    AppVersion = gameVersion,
                };
                client.LocalPlayer.NickName = string.IsNullOrEmpty(nickName)
                    ? nickName
                    : "usr" + SupportClass.ThreadSafeRandom.Next() % 99;
            }

            client.EventReceived += ClientOnEventReceived;
            client.StateChanged += ClientOnStateChanged;
        }

        private void ClientOnStateChanged(ClientState lastState, ClientState currentState)
        {
            switch (currentState)
            {
                case ClientState.ConnectedToMasterServer:
                    // Once the client does connect to the master immediately redirect to the room.
                    if (currentState == ClientState.ConnectedToMasterServer)
                    {
                        var enterRoomParams = new EnterRoomParams()
                        {
                            RoomName = roomName,
                            RoomOptions = new RoomOptions()
                            {
                                MaxPlayers = maxPlayers,
                            }
                        };

                        var success = isHostOrServer
                            ? client.OpCreateRoom(enterRoomParams)
                            : client.OpJoinRoom(enterRoomParams);
                        if (!success)
                        {
                            connectTask.IsDone = true;
                            connectTask.Success = false;
                            connectTask.TransportException =
                                new InvalidOperationException("Unable to create or join room.");
                        }
                    }

                    break;

                case ClientState.Joined:
                    if (attachSupportLogger)
                    {
                        var logger = gameObject.GetComponent<SupportLogger>() ?? gameObject.AddComponent<SupportLogger>();
                        logger.Client = client;
                        client.ConnectionCallbackTargets.Add(logger);
                    }
  
                    // Client connected to the room successfully, connection process is completed
                    connectTask.IsDone = true;
                    connectTask.Success = true;
                    break;
            }
        }

        private void ClientOnEventReceived(EventData eventData)
        {
   
            var clientId = GetMLAPIClientId(eventData.Sender, false);
            var localClientId = GetMLAPIClientId(client.LocalPlayer.ActorNumber, false);
            var isRelevantConnectionUpdateMessage =
                isHostOrServer ^ clientId == localClientId; // Clients should ignore connection events from other clients, server should ignore its own connection event.

            NetEventType netEvent = NetEventType.Nothing;
            ArraySegment<byte> payload = default;
            string channelName = default;
            float receiveTime = Time.realtimeSinceStartup;

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
                    if (eventData.Code >= 200)
                    {
                        return;
                    }

                    byte[] array = (byte[])eventData.CustomData;

                    if (eventData.Code == batchedTransportEventCode)
                    {
                        using (MemoryStream stream = new MemoryStream(array))
                        {
                            using (PooledBitReader reader = PooledBitReader.Get(stream))
                            {
                                while (stream.Position < stream.Length)
                                {
                                    byte channelId = reader.ReadByteDirect();
                                    var length = reader.ReadInt32Packed();
                                    var dataArray = reader.ReadByteArray(null, length);

                                    InvokeOnTransportEvent(NetEventType.Data, clientId, channelIdToName[channelId], new ArraySegment<byte>(dataArray, 0, dataArray.Length), receiveTime);
                                }
                            }
                        }
                        return;
                    }

                    netEvent = NetEventType.Data;
                    payload = new ArraySegment<byte>(array);
                    channelName = channelIdToName[eventData.Code];
                    break;
            }
            if (netEvent == NetEventType.Nothing) return;
            InvokeOnTransportEvent(netEvent, clientId, channelName, payload, receiveTime);
        }

        private SendOptions MLAPIChannelTypeToSendOptions(ChannelType type)
        {
            switch (type)
            {
                case ChannelType.Unreliable:
                    return SendOptions.SendUnreliable;
                default:
                    return SendOptions.SendReliable;
            }
        }

        private ulong GetMLAPIClientId(int photonId, bool isServer)
        {
            if (isServer)
            {
                return 0;
            }
            else
            {
                return (ulong) (photonId + 1);
            }
        }

        private int GetPhotonRealtimeId(ulong clientId)
        {
            if (clientId == 0)
            {
                return client.CurrentRoom.masterClientId;
            }
            else
            {
                return (int) (clientId - 1);
            }
        }

        private class SendQueue
        {
            private MemoryStream stream;

            /// <summary>
            /// The size of the send queue.
            /// </summary>
            public int Size { get; }

            public SendQueue(int size)
            {
                Size = size;
                byte[] buffer = new byte[size];
                stream = new MemoryStream(buffer, 0, buffer.Length, true, true);
            }

            internal bool AddEvent(byte channelId, ArraySegment<byte> data)
            {
                if (stream.Position + data.Count + 4 > Size)
                {
                    return false;
                }

                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteByte(channelId);
                    writer.WriteInt32Packed(data.Count);
                    Array.Copy(data.Array, data.Offset, stream.GetBuffer(), stream.Position, data.Count);
                    stream.Position += data.Count;
                }

                return true;
            }

            internal void Clear()
            {
                stream.Position = 0;
            }

            internal bool IsEmpty()
            {
                return stream.Position == 0;
            }

            internal ArraySegment<byte> GetData()
            {
                return new ArraySegment<byte>(stream.GetBuffer(), 0, (int) stream.Position);
            }
        }

        private struct RealtimeChannel
        {
            public byte Id;
            public string Name;
            public SendOptions SendMode;
        }
    }
}