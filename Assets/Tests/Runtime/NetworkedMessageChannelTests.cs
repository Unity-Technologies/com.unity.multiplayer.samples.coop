using System;
using System.Collections;
using NUnit.Framework;
using Unity.BossRoom.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;
using VContainer;
using Assert = UnityEngine.Assertions.Assert;

namespace Unity.BossRoom.Tests.Runtime
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

        IObjectResolver[] m_ClientScopes;
        IObjectResolver m_ServerScope;

        int m_NbMessagesReceived;

        protected override IEnumerator OnSetup()
        {
            m_NbMessagesReceived = 0;
            return base.OnSetup();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_ClientScopes = new IObjectResolver[NumberOfClients];
            for (int i = 0; i < NumberOfClients; i++)
            {
                var clientBuilder = new ContainerBuilder();
                clientBuilder.RegisterInstance(m_ClientNetworkManagers[i]);
                m_ClientScopes[i] = clientBuilder.Build();
                m_ClientNetworkManagers[i].NetworkConfig.EnableSceneManagement = false;
            }

            var serverBuilder = new ContainerBuilder();
            serverBuilder.RegisterInstance(m_ServerNetworkManager);
            m_ServerScope = serverBuilder.Build();
            m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = false;

            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnTearDown()
        {
            m_Subscriptions.Dispose();
            for (int i = 0; i < NumberOfClients; i++)
            {
                m_ClientScopes[i].Dispose();
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
                m_ClientScopes[i].Inject(emptyMessageChannelClients[i]);
            }

            emptyMessageChannelServer = new NetworkedMessageChannel<T>();
            m_ServerScope.Inject(emptyMessageChannelServer);

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
                        Assert.AreEqual(expectedValue, message, "Message received with unexpected value.");
                    }));
                }
            }

            for (int j = 0; j < nbSubscribers; j++)
            {
                var numSub = j;
                m_Subscriptions.Add(emptyMessageChannelServer.Subscribe(message =>
                {
                    Debug.Log($"Received message on server in subscription {numSub}.");
                    m_NbMessagesReceived++;
                    Assert.AreEqual(expectedValue, message, "Message received with unexpected value.");
                }));
            }
        }

        [UnityTest]
        public IEnumerator EmptyNetworkedMessageIsReceivedByAllSubscribersOnAllClientsAndServer([Values(0, 1, 2)] int nbClients, [Values(0, 1, 2)] int nbSubscribers)
        {
            InitializeNetworkedMessageChannels(nbClients, nbSubscribers, new EmptyMessage(), out var emptyMessageChannelClients, out var emptyMessageChannelServer);

            emptyMessageChannelServer.Publish(new EmptyMessage());

            // wait for the custom named message to be sent on the server and received on the clients
            yield return null;
            yield return null;

            Assert.AreEqual((nbClients + 1) * nbSubscribers, m_NbMessagesReceived);

        }

        [UnityTest]
        public IEnumerator NetworkedMessageContentIsProperlyReceivedOnAllClientsAndServer([Values(0, 1, 2)] int nbClients, [Values(0, 1, 2)] int nbSubscribers)
        {
            InitializeNetworkedMessageChannels(nbClients, nbSubscribers, new GenericMessage() { value = true }, out var genericMessageChannelClients, out var genericMessageChannelServer);

            genericMessageChannelServer.Publish(new GenericMessage() { value = true });

            // wait for the custom named message to be sent on the server and received on the client
            yield return null;
            yield return null;

            Assert.AreEqual((nbClients + 1) * nbSubscribers, m_NbMessagesReceived);
        }

        [UnityTest]
        public IEnumerator NetworkedMessagesAreStillReceivedAfterNetworkManagerShutsDownAndRestarts([Values(0, 1, 2)] int nbClients, [Values(0, 1, 2)] int nbSubscribers)
        {
            InitializeNetworkedMessageChannels(nbClients, nbSubscribers, new EmptyMessage(), out var emptyMessageChannelClients, out var emptyMessageChannelServer);

            emptyMessageChannelServer.Publish(new EmptyMessage());

            // wait for the custom named message to be sent on the server and received on the clients
            yield return null;
            yield return null;

            Assert.AreEqual((nbClients + 1) * nbSubscribers, m_NbMessagesReceived);

            m_NbMessagesReceived = 0;

            m_PlayerPrefab.SetActive(false); // to prevent NM from destroying the prefab on shutdown. This flow isn't great and is hackish, should be reworked
            // Shutdown the server and clients
            m_ServerNetworkManager.Shutdown();
            m_ClientNetworkManagers[0].Shutdown();
            m_ClientNetworkManagers[1].Shutdown();

            yield return new WaitWhile(() => m_ServerNetworkManager.ShutdownInProgress);
            yield return new WaitWhile(() => m_ClientNetworkManagers[0].ShutdownInProgress);
            yield return new WaitWhile(() => m_ClientNetworkManagers[1].ShutdownInProgress);

            m_PlayerPrefab.SetActive(true); // reactivating after destroy prevention

            // Restart the server and clients
            m_ServerNetworkManager.StartHost();
            NetcodeIntegrationTestHelpers.StartOneClient(m_ClientNetworkManagers[0]);
            NetcodeIntegrationTestHelpers.StartOneClient(m_ClientNetworkManagers[1]);

            yield return WaitForClientsConnectedOrTimeOut();

            // Test sending a message a second time
            emptyMessageChannelServer.Publish(new EmptyMessage());

            // wait for the custom named message to be sent on the server and received on the clients
            yield return null;
            yield return null;

            Assert.AreEqual((nbClients + 1) * nbSubscribers, m_NbMessagesReceived);
        }

        [UnityTest]
        public IEnumerator NetworkedMessagesAreReceivedIfClientsSubscribeBeforeConnecting([Values(0, 1, 2)] int nbClients, [Values(0, 1, 2)] int nbSubscribers)
        {
            // Shutdown the clients
            NetcodeIntegrationTestHelpers.StopOneClient(m_ClientNetworkManagers[0], false);
            NetcodeIntegrationTestHelpers.StopOneClient(m_ClientNetworkManagers[1], false);

            yield return new WaitWhile(() => m_ClientNetworkManagers[0].ShutdownInProgress);
            yield return new WaitWhile(() => m_ClientNetworkManagers[1].ShutdownInProgress);

            InitializeNetworkedMessageChannels(nbClients, nbSubscribers, new EmptyMessage(), out var emptyMessageChannelClients, out var emptyMessageChannelServer);

            // Restart the clients
            NetcodeIntegrationTestHelpers.StartOneClient(m_ClientNetworkManagers[0]);
            NetcodeIntegrationTestHelpers.StartOneClient(m_ClientNetworkManagers[1]);

            yield return WaitForClientsConnectedOrTimeOut();

            emptyMessageChannelServer.Publish(new EmptyMessage());

            // wait for the custom named message to be sent on the server and received on the clients
            yield return null;
            yield return null;

            Assert.AreEqual((nbClients + 1) * nbSubscribers, m_NbMessagesReceived);
        }

        [UnityTest]
        public IEnumerator NetworkedMessagesAreNotReceivedWhenClientsAreShutDown([Values(0, 1, 2)] int nbClients, [Values(0, 1, 2)] int nbSubscribers)
        {
            InitializeNetworkedMessageChannels(nbClients, nbSubscribers, new EmptyMessage(), out var emptyMessageChannelClients, out var emptyMessageChannelServer);

            // Shutdown the clients
            NetcodeIntegrationTestHelpers.StopOneClient(m_ClientNetworkManagers[0], false);
            NetcodeIntegrationTestHelpers.StopOneClient(m_ClientNetworkManagers[1], false);

            yield return new WaitWhile(() => m_ClientNetworkManagers[0].ShutdownInProgress);
            yield return new WaitWhile(() => m_ClientNetworkManagers[1].ShutdownInProgress);

            emptyMessageChannelServer.Publish(new EmptyMessage());

            // wait for the custom named message to be sent on the server and received on the clients
            yield return null;
            yield return null;

            Assert.AreEqual(nbSubscribers, m_NbMessagesReceived);
        }

        [UnityTest]
        public IEnumerator NetworkedMessagesAreNotReceivedWhenServerIsShutDown([Values(0, 1, 2)] int nbClients, [Values(0, 1, 2)] int nbSubscribers)
        {
            InitializeNetworkedMessageChannels(nbClients, nbSubscribers, new EmptyMessage(), out var emptyMessageChannelClients, out var emptyMessageChannelServer);

            // Shutdown the server
            NetcodeIntegrationTestHelpers.StopOneClient(m_ServerNetworkManager, false);

            yield return new WaitWhile(() => m_ServerNetworkManager.ShutdownInProgress);

            LogAssert.Expect(LogType.Error, "Only a server can publish in a NetworkedMessageChannel");
            emptyMessageChannelServer.Publish(new EmptyMessage());

            // wait for the custom named message to be sent on the server and received on the clients
            yield return null;
            yield return null;

            Assert.AreEqual(0, m_NbMessagesReceived);
        }

        [UnityTest]
        public IEnumerator NetworkedMessagesCannotBePublishedFromClient()
        {
            InitializeNetworkedMessageChannels(2, 1, new EmptyMessage(), out var emptyMessageChannelClients, out var emptyMessageChannelServer);

            LogAssert.Expect(LogType.Error, "Only a server can publish in a NetworkedMessageChannel");
            emptyMessageChannelClients[0].Publish(new EmptyMessage());

            // wait for the custom named message to be sent on the server and received on the clients
            yield return null;
            yield return null;

            Assert.AreEqual(0, m_NbMessagesReceived);
        }
    }
}
