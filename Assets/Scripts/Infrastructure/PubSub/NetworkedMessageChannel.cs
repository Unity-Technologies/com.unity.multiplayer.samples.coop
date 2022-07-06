using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure
{
    /// <summary>
    /// This type of message channel allows the server to publish a message that will be sent to clients as well as
    /// being published locally. Clients and the server both can subscribe to it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NetworkedMessageChannel<T> : MessageChannel<T> where T : unmanaged, INetworkSerializeByMemcpy
    {
        NetworkManager m_NetworkManager;

        string m_Name;

        public NetworkedMessageChannel()
        {
            m_Name = $"{typeof(T).FullName}NetworkMessageChannel";
        }

        [Inject]
        void InjectDependencies(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            m_NetworkManager.OnClientConnectedCallback += OnClientConnected;
            if (m_NetworkManager.IsListening)
            {
                RegisterHandler();
            }
        }

        public override void Dispose()
        {
            if (!IsDisposed)
            {
                if (m_NetworkManager != null && m_NetworkManager.CustomMessagingManager != null)
                {
                    m_NetworkManager.CustomMessagingManager.UnregisterNamedMessageHandler(m_Name);
                }
            }
            base.Dispose();
        }

        void OnClientConnected(ulong clientId)
        {
            RegisterHandler();
        }

        void RegisterHandler()
        {
            // Only register message handler on clients
            if (!m_NetworkManager.IsServer)
            {
                m_NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(m_Name, ReceiveMessageThroughNetwork);
            }
        }

        public override void Publish(T message)
        {
            if (m_NetworkManager.IsServer)
            {
                // send message to clients, then publish locally
                SendMessageThroughNetwork(message);
                base.Publish(message);
            }
            else
            {
                Debug.LogError("Only a server can publish in a NetworkedMessageChannel");
            }
        }

        void SendMessageThroughNetwork(T message)
        {
            var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize<T>(), Allocator.Temp);
            writer.WriteValueSafe(message);
            m_NetworkManager.CustomMessagingManager.SendNamedMessageToAll(m_Name, writer);
        }

        void ReceiveMessageThroughNetwork(ulong clientID, FastBufferReader reader)
        {
            reader.ReadValueSafe(out T message);
            base.Publish(message);
        }
    }
}
