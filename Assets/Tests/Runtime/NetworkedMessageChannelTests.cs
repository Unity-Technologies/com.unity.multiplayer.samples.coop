using System.Collections;
using NUnit.Framework;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace Unity.Multiplayer.Samples.BossRoom.Tests.Runtime
{
    public class NetworkedMessageChannelTests : NetcodeIntegrationTest
    {
        struct EmptyMessage : INetworkSerializeByMemcpy { }

        struct GenericMessage : INetworkSerializeByMemcpy
        {
            public bool value;
        }

        protected override int NumberOfClients => 2;

        DisposableGroup m_Subscriptions;

        DIScope[] m_ClientScopes;
        DIScope m_ServerScope;

        static int[] s_NbClients = { 0, 1, 2 };
        static int[] s_NbSubs = { 0, 1, 2 };

        int m_NbMessagesReceived;

        protected override IEnumerator OnSetup()
        {
            m_NbMessagesReceived = 0;
            return base.OnSetup();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_ClientScopes = new DIScope[NumberOfClients];
            for (int i = 0; i < NumberOfClients; i++)
            {
                m_ClientScopes[i] = new DIScope();
                m_ClientScopes[i].BindInstanceAsSingle(m_ClientNetworkManagers[i]);
                m_ClientScopes[i].FinalizeScopeConstruction();
            }

            m_ServerScope = new DIScope();
            m_ServerScope.BindInstanceAsSingle(m_ServerNetworkManager);
            m_ServerScope.FinalizeScopeConstruction();

            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnTearDown()
        {
            m_Subscriptions.Dispose();
            for (int i = 0; i < NumberOfClients; i++)
            {
                m_ClientScopes[i] = new DIScope();
                m_ClientScopes[i].BindInstanceAsSingle(m_ClientNetworkManagers[i]);
                m_ClientScopes[i].FinalizeScopeConstruction();
            }
            m_ServerScope.Dispose();
            return base.OnTearDown();
        }

        void InitializeNetworkedMessageChannels<T>(int nbClients, int nbSubscribers, T expectedValue, out NetworkedMessageChannel<T>[] emptyMessageChannelClients, out NetworkedMessageChannel<T> emptyMessageChannelServer) where T : unmanaged, INetworkSerializeByMemcpy
        {
            emptyMessageChannelClients = new NetworkedMessageChannel<T>[nbClients];
            for (int i = 0; i < nbClients; i++)
            {
                emptyMessageChannelClients[i] = new NetworkedMessageChannel<T>();
                m_ClientScopes[i].InjectIn(emptyMessageChannelClients[i]);
            }

            emptyMessageChannelServer = new NetworkedMessageChannel<T>();
            m_ServerScope.InjectIn(emptyMessageChannelServer);

            m_Subscriptions = new DisposableGroup();
            for (int i = 0; i < nbClients; i++)
            {
                for (int j = 0; j < nbSubscribers; j++)
                {
                    var numClient = i;
                    var numSub = j;
                    m_Subscriptions.Add(emptyMessageChannelClients[i].Subscribe(message =>
                    {
                        Debug.Log($"Received message on client {numClient} in subscription {numSub}.");
                        m_NbMessagesReceived++;
                        Assert.AreEqual(message, expectedValue);
                    }));
                }
            }
        }

        [UnityTest]
        public IEnumerator EmptyNetworkedMessageIsReceivedByAllSubscribersOnAllClients([ValueSource(nameof(s_NbClients))] int nbClients, [ValueSource(nameof(s_NbSubs))] int nbSubscribers)
        {
            InitializeNetworkedMessageChannels(nbClients, nbSubscribers, new EmptyMessage(), out var emptyMessageChannelClients, out var emptyMessageChannelServer);

            emptyMessageChannelServer.Publish(new EmptyMessage());

            // wait for the custom named message to be sent on the server and received on the clients
            yield return null;
            yield return null;

            Assert.AreEqual(nbClients * nbSubscribers, m_NbMessagesReceived);

        }

        [UnityTest]
        public IEnumerator NetworkedMessageContentIsProperlyReceived([ValueSource(nameof(s_NbClients))] int nbClients, [ValueSource(nameof(s_NbSubs))] int nbSubscribers)
        {
            InitializeNetworkedMessageChannels(nbClients, nbSubscribers, new GenericMessage() { value = true }, out var genericMessageChannelClients, out var genericMessageChannelServer);

            genericMessageChannelServer.Publish(new GenericMessage() { value = true });

            // wait for the custom named message to be sent on the server and received on the client
            yield return null;
            yield return null;

            Assert.AreEqual(nbClients * nbSubscribers, m_NbMessagesReceived);
        }

        [UnityTest]
        public IEnumerator NetworkedMessagesAreStillReceivedAfterNetworkManagerShutsDownAndRestarts([ValueSource(nameof(s_NbClients))] int nbClients, [ValueSource(nameof(s_NbSubs))] int nbSubscribers)
        {
            InitializeNetworkedMessageChannels(nbClients, nbSubscribers, new EmptyMessage(), out var emptyMessageChannelClients, out var emptyMessageChannelServer);

            emptyMessageChannelServer.Publish(new EmptyMessage());

            // wait for the custom named message to be sent on the server and received on the clients
            yield return null;
            yield return null;

            Assert.AreEqual(nbClients * nbSubscribers, m_NbMessagesReceived);

            m_NbMessagesReceived = 0;

            // Shutdown the server and clients
            m_ServerNetworkManager.Shutdown();
            m_ClientNetworkManagers[0].Shutdown();
            m_ClientNetworkManagers[1].Shutdown();

            yield return new WaitWhile(() => m_ServerNetworkManager.ShutdownInProgress);
            yield return new WaitWhile(() => m_ClientNetworkManagers[0].ShutdownInProgress);
            yield return new WaitWhile(() => m_ClientNetworkManagers[1].ShutdownInProgress);

            // Restart the server and clients
            m_ServerNetworkManager.StartServer();
            m_ClientNetworkManagers[0].StartClient();
            m_ClientNetworkManagers[1].StartClient();

            yield return WaitForClientsConnectedOrTimeOut();

            // Test sending a message a second time
            emptyMessageChannelServer.Publish(new EmptyMessage());

            // wait for the custom named message to be sent on the server and received on the clients
            yield return null;
            yield return null;

            Assert.AreEqual(nbClients * nbSubscribers, m_NbMessagesReceived);
        }
    }
}
