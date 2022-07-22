using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Multiplayer.Samples.BossRoom.Shared;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;

namespace Unity.Multiplayer.Samples.BossRoom.Tests.Runtime
{
    public class ConnectionManagementTests : NetcodeIntegrationTest
    {
        const string k_FailedToConnectToServerErrorMessage = "Failed to connect to server.";

        class ConnectionManagementTestsLifeTimeScope : LifetimeScope
        {
            public NetworkManager NetworkManager;
            public ConnectionManager ConnectionManager;
            public UpdateRunner UpdateRunner;

            protected override void Awake() { }

            protected override void Configure(IContainerBuilder builder)
            {
                builder.RegisterComponent(NetworkManager);
                builder.RegisterComponent(ConnectionManager);
                builder.RegisterComponent(UpdateRunner);
                builder.RegisterInstance(new BufferedMessageChannel<ConnectStatus>()).AsImplementedInterfaces();
                builder.RegisterInstance(new MessageChannel<UnityServiceErrorMessage>()).AsImplementedInterfaces();
                builder.RegisterInstance(new NetworkedMessageChannel<ConnectionEventMessage>()).AsImplementedInterfaces();
                builder.RegisterInstance(new MessageChannel<ReconnectMessage>()).AsImplementedInterfaces();
                builder.RegisterInstance(new BufferedMessageChannel<LobbyListFetchedMessage>()).AsImplementedInterfaces();
                builder.Register<LocalLobby>(Lifetime.Singleton);
                builder.Register<LocalLobbyUser>(Lifetime.Singleton);
                builder.Register<ProfileManager>(Lifetime.Singleton);
                builder.RegisterEntryPoint<LobbyServiceFacade>(Lifetime.Singleton).AsSelf();
            }
        }

        class SceneLoaderWrapperStub : SceneLoaderWrapper
        {
            public override void Awake()
            {
                Instance = this;
            }

            public override void Start() { }

            public override void AddOnSceneEventCallback() { }

            public override void LoadScene(string sceneName, bool useNetworkSceneManager, LoadSceneMode loadSceneMode = LoadSceneMode.Single) { }
        }

        protected override int NumberOfClients => 2;

        ConnectionManagementTestsLifeTimeScope[] m_ClientScopes;
        ConnectionManagementTestsLifeTimeScope m_ServerScope;

        ConnectionManager[] m_ClientConnectionManagers;
        ConnectionManager m_ServerConnectionManager;

        List<ConnectionState> m_ServerConnectionStateSequence;
        List<ConnectionState>[] m_ClientConnectionStateSequences;


        protected override bool CanStartServerAndClients()
        {
            return false;
        }

        static void InitializeInstance(string name, NetworkManager networkManager, List<ConnectionState> connectionStateSequence, GameObject playerGameObject, out ConnectionManager connectionManager, out ConnectionManagementTestsLifeTimeScope scope)
        {
            var serverConnectionManagerGO = new GameObject($"ConnectionManager - {name}");
            connectionManager = serverConnectionManagerGO.AddComponent<ConnectionManager>();

            networkManager.NetworkConfig.ConnectionApproval = true;
            networkManager.NetworkConfig.PlayerPrefab = playerGameObject;

            var serverUpdateRunnerGO = new GameObject($"UpdateRunner - {name}");
            var serverUpdateRunner = serverUpdateRunnerGO.AddComponent<UpdateRunner>();

            var serverLifeTimeScopeGO = new GameObject($"LifeTimeScope - {name}");
            scope = serverLifeTimeScopeGO.AddComponent<ConnectionManagementTestsLifeTimeScope>();
            scope.NetworkManager = networkManager;
            scope.ConnectionManager = connectionManager;
            scope.UpdateRunner = serverUpdateRunner;
            scope.Build();
            connectionManager.m_OnStateChanged += connectionStateSequence.Add;
        }

        protected override void OnServerAndClientsCreated()
        {
            var sceneLoaderWrapperGO = new GameObject("SceneLoader");
            sceneLoaderWrapperGO.AddComponent<SceneLoaderWrapperStub>();

            var playerPrefabGO = new GameObject("PlayerObject");
            var networkObject = playerPrefabGO.AddComponent<NetworkObject>();
            networkObject.DontDestroyWithOwner = true;
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);

            m_ClientScopes = new ConnectionManagementTestsLifeTimeScope[NumberOfClients];
            m_ClientConnectionManagers = new ConnectionManager[NumberOfClients];
            m_ClientConnectionStateSequences = new List<ConnectionState>[NumberOfClients];
            for (var i = 0; i < NumberOfClients; i++)
            {
                var clientId = i;
                m_ClientConnectionStateSequences[clientId] = new List<ConnectionState>();
                InitializeInstance($"Client{clientId}", m_ClientNetworkManagers[clientId], m_ClientConnectionStateSequences[clientId], playerPrefabGO, out m_ClientConnectionManagers[clientId], out m_ClientScopes[clientId]);
            }

            m_ServerConnectionStateSequence = new List<ConnectionState>();
            InitializeInstance("Server", m_ServerNetworkManager, m_ServerConnectionStateSequence, playerPrefabGO, out m_ServerConnectionManager, out m_ServerScope);

            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnTearDown()
        {
            m_ServerConnectionManager.RequestShutdown();

            yield return new WaitWhile(() => m_ServerNetworkManager.IsListening);

            for (var i = 0; i < NumberOfClients; i++)
            {
                var clientId = i;
                yield return new WaitWhile(() => m_ClientNetworkManagers[clientId].IsListening);
            }

            m_ServerScope.Dispose();

            for (var i = 0; i < NumberOfClients; i++)
            {
                m_ClientScopes[i].Dispose();
            }

            foreach (var sceneGameObject in GameObject.FindObjectsOfType<GameObject>())
            {
                GameObject.DestroyImmediate(sceneGameObject);
            }
            yield return base.OnTearDown();
        }

        void StartHost()
        {
            m_ServerConnectionManager.StartHostIp("server", "127.0.0.1", 9998);
        }

        IEnumerator ConnectClients()
        {
            for (var i = 0; i < NumberOfClients; i++)
            {
                m_ClientConnectionManagers[i].StartClientIp($"client{i}", "127.0.0.1", 9998);
            }

            yield return WaitForClientsConnectedOrTimeOut(m_ClientNetworkManagers);
        }

        void SetUniqueProfilesForEachClient()
        {
            for (var i = 0; i < NumberOfClients; i++)
            {
                var profileManager = m_ClientScopes[i].Container.Resolve<ProfileManager>();
                profileManager.Profile = $"Client{i}";
            }
        }

        [Test]
        public void StartHost_Success()
        {
            StartHost();
            Assert.IsTrue(m_ServerNetworkManager.IsHost);
            var expectedServerConnectionStateSequence = new List<ConnectionState>();
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_StartingHost);
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_Hosting);
            Assert.AreEqual(expectedServerConnectionStateSequence, m_ServerConnectionStateSequence);
        }

        [UnityTest]
        public IEnumerator StartHostAndConnectClients_Success()
        {
            StartHost();
            Assert.IsTrue(m_ServerNetworkManager.IsHost);

            SetUniqueProfilesForEachClient();

            yield return ConnectClients();
            for (var i = 0; i < NumberOfClients; i++)
            {
                Assert.IsTrue(m_ClientNetworkManagers[i].IsConnectedClient);
            }

            var expectedServerConnectionStateSequence = new List<ConnectionState>();
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_StartingHost);
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_Hosting);
            Assert.AreEqual(expectedServerConnectionStateSequence, m_ServerConnectionStateSequence);

            for (var i = 0; i < NumberOfClients; i++)
            {
                var expectedClientConnectionStateSequence = new List<ConnectionState>();
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnecting);
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnected);
                Assert.AreEqual(expectedClientConnectionStateSequence, m_ClientConnectionStateSequences[i]);
            }
        }

        [UnityTest]
        public IEnumerator UserRequestedHostShutdownAfterClientsConnected_ClientsDisconnectedWithReason()
        {
            StartHost();
            Assert.IsTrue(m_ServerNetworkManager.IsHost);

            SetUniqueProfilesForEachClient();

            yield return ConnectClients();
            for (var i = 0; i < NumberOfClients; i++)
            {
                Assert.IsTrue(m_ClientNetworkManagers[i].IsConnectedClient);
            }

            m_ServerConnectionManager.RequestShutdown();

            yield return new WaitWhile(() => m_ServerNetworkManager.IsListening);

            Assert.IsFalse(m_ServerNetworkManager.IsHost);

            for (var i = 0; i < NumberOfClients; i++)
            {
                var clientId = i;
                yield return new WaitWhile(() => m_ClientNetworkManagers[clientId].IsListening);
                Assert.IsFalse(m_ClientNetworkManagers[clientId].IsConnectedClient);
            }

            var expectedServerConnectionStateSequence = new List<ConnectionState>();
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_StartingHost);
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_Hosting);
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_Offline);
            Assert.AreEqual(expectedServerConnectionStateSequence, m_ServerConnectionStateSequence);

            for (var i = 0; i < NumberOfClients; i++)
            {
                var expectedClientConnectionStateSequence = new List<ConnectionState>();
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnecting);
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnected);
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_DisconnectingWithReason);
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_Offline);
                Assert.AreEqual(expectedClientConnectionStateSequence, m_ClientConnectionStateSequences[i]);
            }
        }

        [UnityTest]
        public IEnumerator AttemptingToConnectWithSamePlayerId_ClientsDisconnectedWithReason()
        {
            StartHost();
            Assert.IsTrue(m_ServerNetworkManager.IsHost);

            for (var i = 0; i < NumberOfClients; i++)
            {
                var profileManager = m_ClientScopes[i].Container.Resolve<ProfileManager>();
                profileManager.Profile = $"Client";
            }

            yield return ConnectClients();

            Assert.IsTrue(m_ClientNetworkManagers[0].IsConnectedClient);
            for (var i = 1; i < NumberOfClients; i++)
            {
                Assert.IsFalse(m_ClientNetworkManagers[i].IsConnectedClient);
            }

            var expectedServerConnectionStateSequence = new List<ConnectionState>();
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_StartingHost);
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_Hosting);
            Assert.AreEqual(expectedServerConnectionStateSequence, m_ServerConnectionStateSequence);

            for (var i = 0; i < NumberOfClients; i++)
            {
                var expectedClientConnectionStateSequence = new List<ConnectionState>();
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnecting);
                if (i == 0)
                {
                    expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnected);
                }
                else
                {
                    expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_DisconnectingWithReason);
                    expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_Offline);
                }
                Assert.AreEqual(expectedClientConnectionStateSequence, m_ClientConnectionStateSequences[i]);
            }
        }

        [UnityTest]
        public IEnumerator ClientConnectingWithoutHost_ConnectionFailed()
        {
            SetUniqueProfilesForEachClient();

            for (var i = 0; i < NumberOfClients; i++)
            {
                LogAssert.Expect(LogType.Error, k_FailedToConnectToServerErrorMessage);
            }

            yield return ConnectClients();

            for (var i = 0; i < NumberOfClients; i++)
            {
                Assert.IsFalse(m_ClientNetworkManagers[i].IsConnectedClient);
            }

            for (var i = 0; i < NumberOfClients; i++)
            {
                var expectedClientConnectionStateSequence = new List<ConnectionState>();
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnecting);
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_Offline);
                Assert.AreEqual(expectedClientConnectionStateSequence, m_ClientConnectionStateSequences[i]);
            }
        }

        [UnityTest]
        public IEnumerator UnexpectedClientDisconnect_ClientReconnectingSuccessfully()
        {
            StartHost();
            Assert.IsTrue(m_ServerNetworkManager.IsHost);

            SetUniqueProfilesForEachClient();

            yield return ConnectClients();
            for (var i = 0; i < NumberOfClients; i++)
            {
                Assert.IsTrue(m_ClientNetworkManagers[i].IsConnectedClient);
            }

            // wait for client synchronization to be over server-side before shutting down client
            yield return null;
            yield return null;

            // Disconnecting the client at the transport level
            m_ClientNetworkManagers[0].NetworkConfig.NetworkTransport.DisconnectLocalClient();

            // Waiting until shutdown is complete
            yield return new WaitWhile(() => m_ClientNetworkManagers[0].ShutdownInProgress);
            Assert.IsFalse(m_ClientNetworkManagers[0].IsConnectedClient);

            // Waiting for client to automatically reconnect
            yield return WaitForClientsConnectedOrTimeOut();
            Assert.IsTrue(m_ClientNetworkManagers[0].IsConnectedClient);

            var expectedServerConnectionStateSequence = new List<ConnectionState>();
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_StartingHost);
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_Hosting);
            Assert.AreEqual(expectedServerConnectionStateSequence, m_ServerConnectionStateSequence);

            for (var i = 0; i < NumberOfClients; i++)
            {
                var expectedClientConnectionStateSequence = new List<ConnectionState>();
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnecting);
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnected);
                if (i == 0)
                {
                    expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientReconnecting);
                    expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnected);
                }
                Assert.AreEqual(expectedClientConnectionStateSequence, m_ClientConnectionStateSequences[i]);
            }

        }

        [UnityTest]
        public IEnumerator UnexpectedServerShutdown_ClientsFailToReconnect()
        {
            StartHost();
            Assert.IsTrue(m_ServerNetworkManager.IsHost);

            SetUniqueProfilesForEachClient();

            yield return ConnectClients();
            for (var i = 0; i < NumberOfClients; i++)
            {
                Assert.IsTrue(m_ClientNetworkManagers[i].IsConnectedClient);
            }

            // Shutting down the server
            m_ServerNetworkManager.Shutdown();
            // ConnectionManager should also transition to Offline state normally, but OnClientDisconnectCallback is not
            // invoked on the host when shutting down, so transitioning here manually
            m_ServerConnectionManager.ChangeState(m_ServerConnectionManager.m_Offline);
            yield return new WaitWhile(() => m_ServerNetworkManager.ShutdownInProgress);
            Assert.IsFalse(m_ServerNetworkManager.IsListening);

            // Waiting until shutdown is complete for the client as well
            for (var i = 0; i < NumberOfClients; i++)
            {
                yield return new WaitWhile(() => m_ClientNetworkManagers[i].ShutdownInProgress);
                Assert.IsFalse(m_ClientNetworkManagers[i].IsConnectedClient);
            }

            // Waiting for clients to fail to automatically reconnect
            for (var i = 0; i < NumberOfClients; i++)
            {
                // expecting that error twice per client since there are two reconnect attempts
                LogAssert.Expect(LogType.Error, k_FailedToConnectToServerErrorMessage);
                LogAssert.Expect(LogType.Error, k_FailedToConnectToServerErrorMessage);
            }
            yield return WaitForClientsConnectedOrTimeOut();
            for (var i = 0; i < NumberOfClients; i++)
            {
                Assert.IsFalse(m_ClientNetworkManagers[i].IsConnectedClient);
            }

            var expectedServerConnectionStateSequence = new List<ConnectionState>();
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_StartingHost);
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_Hosting);
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_Offline);
            Assert.AreEqual(expectedServerConnectionStateSequence, m_ServerConnectionStateSequence);

            for (var i = 0; i < NumberOfClients; i++)
            {
                var expectedClientConnectionStateSequence = new List<ConnectionState>();
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnecting);
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnected);
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientReconnecting);
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_Offline);
                Assert.AreEqual(expectedClientConnectionStateSequence, m_ClientConnectionStateSequences[i]);
            }
        }

        [UnityTest]
        public IEnumerator ClientAndHostChangingRolesBetweenSessions_Success()
        {
            SetUniqueProfilesForEachClient();
            m_ClientConnectionManagers[0].StartHostIp("server", "127.0.0.1", 9998);
            Assert.IsTrue(m_ClientNetworkManagers[0].IsHost);

            m_ServerConnectionManager.StartClientIp("client0", "127.0.0.1", 9998);
            for (var i = 1; i < NumberOfClients; i++)
            {
                m_ClientConnectionManagers[i].StartClientIp($"client{i}", "127.0.0.1", 9998);
            }

            yield return WaitForClientsConnectedOrTimeOut(m_ClientNetworkManagers);
            Assert.IsTrue(m_ServerNetworkManager.IsConnectedClient);
            for (var i = 1; i < NumberOfClients; i++)
            {
                Assert.IsTrue(m_ClientNetworkManagers[i].IsConnectedClient);
            }

            m_ClientConnectionManagers[0].RequestShutdown();
            yield return new WaitWhile(() => m_ClientNetworkManagers[0].IsListening);

            StartHost();
            Assert.IsTrue(m_ServerNetworkManager.IsHost);

            yield return ConnectClients();
            for (var i = 0; i < NumberOfClients; i++)
            {
                Assert.IsTrue(m_ClientNetworkManagers[i].IsConnectedClient);
            }


            Assert.IsFalse(m_ServerNetworkManager.IsHost);

            for (var i = 0; i < NumberOfClients; i++)
            {
                var clientId = i;
                yield return new WaitWhile(() => m_ClientNetworkManagers[clientId].IsListening);
                Assert.IsFalse(m_ClientNetworkManagers[clientId].IsConnectedClient);
            }

            var expectedServerConnectionStateSequence = new List<ConnectionState>();
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_ClientConnecting);
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_ClientConnected);
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_DisconnectingWithReason);
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_Offline);
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_StartingHost);
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_Hosting);
            Assert.AreEqual(expectedServerConnectionStateSequence, m_ServerConnectionStateSequence);

            for (var i = 0; i < NumberOfClients; i++)
            {
                var expectedClientConnectionStateSequence = new List<ConnectionState>();
                if (i == 0)
                {
                    expectedClientConnectionStateSequence.Add(m_ServerConnectionManager.m_StartingHost);
                    expectedClientConnectionStateSequence.Add(m_ServerConnectionManager.m_Hosting);
                }
                else
                {
                    expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnecting);
                    expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnected);
                }
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_DisconnectingWithReason);
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_Offline);
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnecting);
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnected);

                Assert.AreEqual(expectedClientConnectionStateSequence, m_ClientConnectionStateSequences[i]);
            }
        }

        [UnityTest]
        public IEnumerator ClientCancellingWhileConnectingToListeningServer_ConnectionCancelled()
        {
            StartHost();
            Assert.IsTrue(m_ServerNetworkManager.IsHost);

            SetUniqueProfilesForEachClient();

            for (var i = 0; i < NumberOfClients; i++)
            {
                m_ClientConnectionManagers[i].StartClientIp($"client{i}", "127.0.0.1", 9998);
            }

            yield return null;
            yield return null;

            m_ClientConnectionManagers[0].RequestShutdown();

            yield return WaitForClientsConnectedOrTimeOut();

            Assert.IsFalse(m_ClientNetworkManagers[0].IsConnectedClient);

            for (var i = 1; i < NumberOfClients; i++)
            {
                Assert.IsTrue(m_ClientNetworkManagers[i].IsConnectedClient);
            }

            var expectedServerConnectionStateSequence = new List<ConnectionState>();
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_StartingHost);
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_Hosting);
            Assert.AreEqual(expectedServerConnectionStateSequence, m_ServerConnectionStateSequence);

            for (var i = 0; i < NumberOfClients; i++)
            {
                var expectedClientConnectionStateSequence = new List<ConnectionState>();
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnecting);
                if (i == 0)
                {
                    expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_Offline);
                }
                else
                {
                    expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnected);
                }
                Assert.AreEqual(expectedClientConnectionStateSequence, m_ClientConnectionStateSequences[i]);
            }
        }

        [UnityTest]
        public IEnumerator ClientCancellingWhileConnectingToNonExistingServer_NoConnectionError()
        {
            SetUniqueProfilesForEachClient();

            for (var i = 0; i < NumberOfClients; i++)
            {
                m_ClientConnectionManagers[i].StartClientIp($"client{i}", "127.0.0.1", 9998);
            }
            m_ClientConnectionManagers[0].RequestShutdown();

            for (var i = 1; i < NumberOfClients; i++)
            {
                // expecting that error for every client except the one that cancelled its connection
                LogAssert.Expect(LogType.Error, k_FailedToConnectToServerErrorMessage);
            }

            yield return WaitForClientsConnectedOrTimeOut();

            for (var i = 0; i < NumberOfClients; i++)
            {
                Assert.IsFalse(m_ClientNetworkManagers[i].IsConnectedClient);
            }

            var expectedServerConnectionStateSequence = new List<ConnectionState>();
            Assert.AreEqual(expectedServerConnectionStateSequence, m_ServerConnectionStateSequence);

            for (var i = 0; i < NumberOfClients; i++)
            {
                var expectedClientConnectionStateSequence = new List<ConnectionState>();
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_ClientConnecting);
                expectedClientConnectionStateSequence.Add(m_ClientConnectionManagers[i].m_Offline);
                Assert.AreEqual(expectedClientConnectionStateSequence, m_ClientConnectionStateSequences[i]);
            }
        }

    }
}
