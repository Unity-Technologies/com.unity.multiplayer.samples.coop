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

        static int[] s_NbClients = { 1, 2 };
        static int[] s_NbSubs = { 1, 2 };

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

        [UnityTest]
        public IEnumerator EmptyNetworkedMessageIsReceivedByAllSubscribersOnAllClients([ValueSource(nameof(s_NbClients))] int nbClients, [ValueSource(nameof(s_NbSubs))] int nbSubscribers)
        {
            var emptyMessageChannelClients = new NetworkedMessageChannel<EmptyMessage>[nbClients];
            for (int i = 0; i < nbClients; i++)
            {
                emptyMessageChannelClients[i] = new NetworkedMessageChannel<EmptyMessage>();
                m_ClientScopes[i].InjectIn(emptyMessageChannelClients[i]);
            }
            var emptyMessageChannelServer = new NetworkedMessageChannel<EmptyMessage>();
            m_ServerScope.InjectIn(emptyMessageChannelServer);

            var nbMessagesReceived = 0;

            m_Subscriptions = new DisposableGroup();
            for (int i = 0; i < nbClients; i++)
            {
                for (int j = 0; j < nbSubscribers; j++)
                {
                    var numClient = i;
                    var numSub = j;
                    m_Subscriptions.Add(emptyMessageChannelClients[i].Subscribe((message =>
                    {
                        Debug.Log($"Received message on client {numClient} in subscription {numSub}.");
                        nbMessagesReceived++;
                    })));
                }
            }

            emptyMessageChannelServer.Publish(new EmptyMessage());

            // wait for the custom named message to be sent on the server and received on the clients
            yield return null;
            yield return null;

            Assert.AreEqual(nbClients * nbSubscribers, nbMessagesReceived);

        }

        [UnityTest]
        public IEnumerator NetworkedMessageContentIsProperlyReceived()
        {
            var genericMessageChannelClient = new NetworkedMessageChannel<GenericMessage>();
            m_ClientScopes[0].InjectIn(genericMessageChannelClient);
            var genericMessageChannelServer = new NetworkedMessageChannel<GenericMessage>();
            m_ServerScope.InjectIn(genericMessageChannelServer);

            var nbMessagesReceived = 0;

            m_Subscriptions = new DisposableGroup();
            m_Subscriptions.Add(genericMessageChannelClient.Subscribe(OnGenericMessageReceived));

            void OnGenericMessageReceived(GenericMessage message)
            {
                nbMessagesReceived++;
                Assert.IsTrue(message.value);
            }

            genericMessageChannelServer.Publish(new GenericMessage() { value = true });

            // wait for the custom named message to be sent on the server and received on the client
            yield return null;
            yield return null;

            Assert.AreEqual(1, nbMessagesReceived);
        }

        [UnityTest]
        public IEnumerator NetworkedMessagesAreStillReceivedAfterNetworkManagerShutsDownAndRestarts()
        {
            var emptyMessageChannelClient1 = new NetworkedMessageChannel<EmptyMessage>();
            m_ClientScopes[0].InjectIn(emptyMessageChannelClient1);
            var emptyMessageChannelClient2 = new NetworkedMessageChannel<EmptyMessage>();
            m_ClientScopes[1].InjectIn(emptyMessageChannelClient2);
            var emptyMessageChannelServer = new NetworkedMessageChannel<EmptyMessage>();
            m_ServerScope.InjectIn(emptyMessageChannelServer);

            var nbMessagesReceived = 0;

            m_Subscriptions = new DisposableGroup();
            m_Subscriptions.Add(emptyMessageChannelClient1.Subscribe(OnEmptyMessageReceived));
            m_Subscriptions.Add(emptyMessageChannelClient2.Subscribe(OnEmptyMessageReceived));

            void OnEmptyMessageReceived(EmptyMessage message)
            {
                nbMessagesReceived++;
            }

            emptyMessageChannelServer.Publish(new EmptyMessage());

            // wait for the custom named message to be sent on the server and received on the clients
            yield return null;
            yield return null;

            Assert.AreEqual(2, nbMessagesReceived);

            nbMessagesReceived = 0;

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

            Assert.AreEqual(2, nbMessagesReceived);
        }
    }
}
