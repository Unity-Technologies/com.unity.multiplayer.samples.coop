using System.Collections;
using UnityEngine;
using BossRoom.Scripts.Shared.Utilities;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Multiplayer.Samples.Utilities.Tests.Runtime
{
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    public class WaitForEventsTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        protected override bool CanStartServerAndClients()
        {
            return false;
        }

        public WaitForEventsTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        [UnityTest]
        public IEnumerator BasicServerStartedTest()
        {
            var waitForServerIterator = new WaitForServerStarted(m_ServerNetworkManager);
            Assert.True(waitForServerIterator.MoveNext()); // check that this would be waiting since server isn't started

            Assert.That(m_UseHost ? m_ServerNetworkManager.StartHost() : m_ServerNetworkManager.StartServer());
            yield return waitForServerIterator;

            Assert.That(m_ServerNetworkManager.IsServer);
            Assert.That(m_ServerNetworkManager.IsListening);
        }

        [UnityTest]
        public IEnumerator WaiterDoesntMoveForwardIfNoServer()
        {
            Assert.That(m_ServerNetworkManager.IsServer, Is.False);
            Assert.That(m_ServerNetworkManager.IsListening, Is.False);

            var waitForServerIterator = new WaitForServerStarted(m_ServerNetworkManager);

            // do a few update loops iterations to make sure this waits while waiting for server started
            for (int i = 0; i < 10; i++)
            {
                Assert.True(waitForServerIterator.MoveNext());
                yield return null;
            }

            // then moves forward if there's a server
            Assert.That(m_UseHost ? m_ServerNetworkManager.StartHost() : m_ServerNetworkManager.StartServer());

            yield return waitForServerIterator;

            Assert.True(m_ServerNetworkManager.IsServer);
            Assert.True(m_ServerNetworkManager.IsListening);
        }

        [UnityTest]
        public IEnumerator ServerAlreadyStarted()
        {
            Assert.That(m_UseHost ? m_ServerNetworkManager.StartHost() : m_ServerNetworkManager.StartServer());

            yield return null; // wait for server to start

            Assert.True(m_ServerNetworkManager.IsServer);
            Assert.True(m_ServerNetworkManager.IsListening);

            var waitForServerIterator = new WaitForServerStarted(m_ServerNetworkManager);
            Assert.False(waitForServerIterator.keepWaiting);
            yield return new WaitForServerStarted(m_ServerNetworkManager);
        }

        [UnityTest]
        public IEnumerator StillWorksAfterServerRestart()
        {
            Assert.That(m_UseHost ? m_ServerNetworkManager.StartHost() : m_ServerNetworkManager.StartServer());
            yield return new WaitForServerStarted(m_ServerNetworkManager);

            Assert.That(m_ServerNetworkManager.IsServer);
            Assert.That(m_ServerNetworkManager.IsListening);

            // Restart
            m_ServerNetworkManager.Shutdown();
            yield return null;

            Assert.False(m_ServerNetworkManager.IsServer);
            Assert.False(m_ServerNetworkManager.IsListening);
            Assert.That(m_UseHost ? m_ServerNetworkManager.StartHost() : m_ServerNetworkManager.StartServer());
            yield return new WaitForServerStarted(m_ServerNetworkManager);

            Assert.That(m_ServerNetworkManager.IsServer);
            Assert.That(m_ServerNetworkManager.IsListening);
        }

        [UnityTest]
        public IEnumerator DoesntDoAnythingWithStartClient()
        {
            var clientNetworkManager = m_ClientNetworkManagers[0];
            Assert.That(clientNetworkManager.StartClient());
            var waitForServerIteratorClientSide = new WaitForServerStarted(clientNetworkManager);

            // do a few update loops iterations to make sure this waits
            int count = 10;
            for (int i = 0; i < count; i++)
            {
                Assert.Throws(typeof(NotServerException), () =>
                {
                    waitForServerIteratorClientSide.MoveNext();
                });
            }

            // make sure we recover from those exceptions
            var waitForServerIterator = new WaitForServerStarted(m_ServerNetworkManager);
            Assert.True(waitForServerIterator.MoveNext()); // check that this would be waiting since server isn't started

            Assert.That(m_UseHost ? m_ServerNetworkManager.StartHost() : m_ServerNetworkManager.StartServer());
            yield return waitForServerIterator;

            Assert.That(m_ServerNetworkManager.IsServer);
            Assert.That(m_ServerNetworkManager.IsListening);
        }
    }
}
