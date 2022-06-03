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
        public IEnumerator NetworkedMessageIsReceivedByAllSubscribersOnSingleClient()
        {
            var emptyMessageChannelClient = new NetworkedMessageChannel<EmptyMessage>(FirstClient);
            var emptyMessageChannelServer = new NetworkedMessageChannel<EmptyMessage>(m_ServerNetworkManager);

            var genericMessageChannelClient = new NetworkedMessageChannel<GenericMessage>(FirstClient);
            var genericMessageChannelServer = new NetworkedMessageChannel<GenericMessage>(m_ServerNetworkManager);

            var subscriptions = new DisposableGroup();
            var nbMessagesReceived = 0;
            subscriptions.Add(emptyMessageChannelClient.Subscribe(OnEmptyMessageReceived));
            subscriptions.Add(emptyMessageChannelClient.Subscribe(OnEmptyMessageReceived2));
            subscriptions.Add(genericMessageChannelClient.Subscribe(OnGenericMessageReceived));

            void OnEmptyMessageReceived(EmptyMessage message)
            {
                nbMessagesReceived++;
            }

            void OnEmptyMessageReceived2(EmptyMessage message)
            {
                nbMessagesReceived++;
            }

            void OnGenericMessageReceived(GenericMessage message)
            {
                nbMessagesReceived++;
                Assert.IsTrue(message.value);
            }

            yield return null;

            emptyMessageChannelServer.Publish(new EmptyMessage());
            emptyMessageChannelServer.Publish(new EmptyMessage());
            genericMessageChannelServer.Publish(new GenericMessage() {value = true});

            yield return new WaitForSeconds(1);

            Assert.AreEqual(5, nbMessagesReceived);

        }
    }
}
