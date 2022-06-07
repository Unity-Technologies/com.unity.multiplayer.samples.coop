using System.Collections;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.Assertions;
using UnityEngine.TestTools;

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

        NetworkManager FirstClient => m_ClientNetworkManagers[0];
        NetworkManager SecondClient => m_ClientNetworkManagers[1];

        DisposableGroup m_Subscriptions;

        DIScope m_FirstClientScope;
        DIScope m_SecondClientScope;
        DIScope m_ServerScope;

        protected override void OnServerAndClientsCreated()
        {
            m_FirstClientScope = new DIScope();
            m_FirstClientScope.BindInstanceAsSingle(FirstClient);
            m_FirstClientScope.FinalizeScopeConstruction();

            m_SecondClientScope = new DIScope();
            m_SecondClientScope.BindInstanceAsSingle(SecondClient);
            m_SecondClientScope.FinalizeScopeConstruction();

            m_ServerScope = new DIScope();
            m_ServerScope.BindInstanceAsSingle(m_ServerNetworkManager);
            m_ServerScope.FinalizeScopeConstruction();

            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnTearDown()
        {
            m_Subscriptions.Dispose();
            m_FirstClientScope.Dispose();
            m_SecondClientScope.Dispose();
            m_ServerScope.Dispose();
            return base.OnTearDown();
        }


        [UnityTest]
        public IEnumerator EmptyNetworkedMessageIsReceivedByAllSubscribersOnMultipleClients()
        {
            var emptyMessageChannelClient1 = new NetworkedMessageChannel<EmptyMessage>();
            m_FirstClientScope.InjectIn(emptyMessageChannelClient1);
            var emptyMessageChannelClient2 = new NetworkedMessageChannel<EmptyMessage>();
            m_SecondClientScope.InjectIn(emptyMessageChannelClient2);
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
        }

        [UnityTest]
        public IEnumerator EmptyNetworkedMessageIsReceivedByAllSubscribersOnSingleClient()
        {
            var emptyMessageChannelClient = new NetworkedMessageChannel<EmptyMessage>();
            m_FirstClientScope.InjectIn(emptyMessageChannelClient);
            var emptyMessageChannelServer = new NetworkedMessageChannel<EmptyMessage>();
            m_ServerScope.InjectIn(emptyMessageChannelServer);

            var nbMessagesReceived = 0;

            m_Subscriptions = new DisposableGroup();
            m_Subscriptions.Add(emptyMessageChannelClient.Subscribe(OnEmptyMessageReceived));
            m_Subscriptions.Add(emptyMessageChannelClient.Subscribe(OnEmptyMessageReceived2));

            void OnEmptyMessageReceived(EmptyMessage message)
            {
                nbMessagesReceived++;
            }

            void OnEmptyMessageReceived2(EmptyMessage message)
            {
                nbMessagesReceived++;
            }

            emptyMessageChannelServer.Publish(new EmptyMessage());

            // wait for the custom named message to be sent on the server and received on the client
            yield return null;
            yield return null;

            Assert.AreEqual(2, nbMessagesReceived);
        }

        [UnityTest]
        public IEnumerator NetworkedMessageContentIsProperlyReceived()
        {
            var genericMessageChannelClient = new NetworkedMessageChannel<GenericMessage>();
            m_FirstClientScope.InjectIn(genericMessageChannelClient);
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
            m_FirstClientScope.InjectIn(emptyMessageChannelClient1);
            var emptyMessageChannelClient2 = new NetworkedMessageChannel<EmptyMessage>();
            m_SecondClientScope.InjectIn(emptyMessageChannelClient2);
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

            // Shutdown the server and clients
            m_ServerNetworkManager.Shutdown();
            FirstClient.Shutdown();
            SecondClient.Shutdown();

            yield return new WaitWhile(() => m_ServerNetworkManager.ShutdownInProgress);
            yield return new WaitWhile(() => FirstClient.ShutdownInProgress);
            yield return new WaitWhile(() => SecondClient.ShutdownInProgress);

            // Restart the server and clients
            m_ServerNetworkManager.StartServer();
            FirstClient.StartClient();
            SecondClient.StartClient();

            yield return WaitForClientsConnectedOrTimeOut();

            // Test sending a message a second time
            emptyMessageChannelServer.Publish(new EmptyMessage());

            // wait for the custom named message to be sent on the server and received on the clients
            yield return null;
            yield return null;

            Assert.AreEqual(4, nbMessagesReceived);
        }
    }
}
