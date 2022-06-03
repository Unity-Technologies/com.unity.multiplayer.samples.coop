using UnityEngine;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Multiplayer.Samples.BossRoom.Tests.Runtime
{
    public class NetworkedMessageChannelTests: NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;
    }
}
