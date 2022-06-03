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
        }

        protected override int NumberOfClients => 2;

        NetworkManager FirstClient => m_ClientNetworkManagers[0];
        NetworkManager SecondClient => m_ClientNetworkManagers[1];

        [UnityTest]
        public IEnumerator NetworkedMessageIsReceivedByAllSubscribersOnSingleClient()
        {
            var emptyMessageChannel = new NetworkedMessageChannel<EmptyMessage>(FirstClient);

            yield return null;

        }
    }
}
