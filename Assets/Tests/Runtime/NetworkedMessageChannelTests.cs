using System.Collections;
using Unity.Collections;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.Assertions;
using UnityEngine.TestTools;

namespace Unity.Multiplayer.Samples.BossRoom.Tests.Runtime
{
    public class NetworkedMessageChannelTests: NetcodeIntegrationTest
    {
        struct EmptyMessage : INetworkSerializeByMemcpy { }

        struct GenericMessage : INetworkSerializeByMemcpy
        {
            public bool value;
        }

        protected override int NumberOfClients => 2;

        NetworkManager FirstClient => m_ClientNetworkManagers[0];
        NetworkManager SecondClient => m_ClientNetworkManagers[1];

        [UnityTest]
        public IEnumerator EmptyNetworkedMessageIsReceivedByAllSubscribersOnMultipleClients()
        {
            var emptyMessageChannelClient1 = new NetworkedMessageChannel<EmptyMessage>(FirstClient);
            var emptyMessageChannelClient2 = new NetworkedMessageChannel<EmptyMessage>(SecondClient);
            var emptyMessageChannelServer = new NetworkedMessageChannel<EmptyMessage>(m_ServerNetworkManager);

            var nbMessagesReceived = 0;

            var subscriptions = new DisposableGroup();
            subscriptions.Add(emptyMessageChannelClient1.Subscribe(OnEmptyMessageReceived));
            subscriptions.Add(emptyMessageChannelClient2.Subscribe(OnEmptyMessageReceived));

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
            var emptyMessageChannelClient = new NetworkedMessageChannel<EmptyMessage>(FirstClient);
            var emptyMessageChannelServer = new NetworkedMessageChannel<EmptyMessage>(m_ServerNetworkManager);

            var nbMessagesReceived = 0;

            var subscriptions = new DisposableGroup();
            subscriptions.Add(emptyMessageChannelClient.Subscribe(OnEmptyMessageReceived));
            subscriptions.Add(emptyMessageChannelClient.Subscribe(OnEmptyMessageReceived2));

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

            var genericMessageChannelClient = new NetworkedMessageChannel<GenericMessage>(FirstClient);
            var genericMessageChannelServer = new NetworkedMessageChannel<GenericMessage>(m_ServerNetworkManager);

            var nbMessagesReceived = 0;

            var subscriptions = new DisposableGroup();
            subscriptions.Add(genericMessageChannelClient.Subscribe(OnGenericMessageReceived));

            void OnGenericMessageReceived(GenericMessage message)
            {
                nbMessagesReceived++;
                Assert.IsTrue(message.value);
            }

            genericMessageChannelServer.Publish(new GenericMessage() {value = true});

            // wait for the custom named message to be sent on the server and received on the client
            yield return null;
            yield return null;

            Assert.AreEqual(1, nbMessagesReceived);

        }
    }
}
