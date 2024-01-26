using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Infrastructure;
using Unity.BossRoom.UnityServices;
using Unity.BossRoom.UnityServices.Lobbies;
using Unity.BossRoom.Utils;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;

namespace Unity.BossRoom.Tests.Runtime
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
                builder.RegisterComponent(new NetworkedMessageChannel<ConnectionEventMessage>()).AsImplementedInterfaces();
                builder.RegisterInstance(new BufferedMessageChannel<ConnectStatus>()).AsImplementedInterfaces();
                builder.RegisterInstance(new MessageChannel<UnityServiceErrorMessage>()).AsImplementedInterfaces();
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

            public override void LoadScene(string sceneName, bool useNetworkSceneManager, LoadSceneMode loadSceneMode = LoadSceneMode.Single) { }
        }

        protected override int NumberOfClients => 2;

        ConnectionManagementTestsLifeTimeScope[] m_ClientScopes;
        ConnectionManagementTestsLifeTimeScope m_ServerScope;

        ConnectionManager[] m_ClientConnectionManagers;
        ConnectionManager m_ServerConnectionManager;

        protected override bool CanStartServerAndClients()
        {
            return false;
        }

        static void InitializeInstance(string name, NetworkManager networkManager, out ConnectionManager connectionManager, out ConnectionManagementTestsLifeTimeScope scope)
        {
            var connectionManagerGO = new GameObject($"ConnectionManager - {name}");
            connectionManager = connectionManagerGO.AddComponent<ConnectionManager>();

            networkManager.NetworkConfig.ConnectionApproval = true;
            networkManager.NetworkConfig.EnableSceneManagement = false;

            var updateRunnerGO = new GameObject($"UpdateRunner - {name}");
            var updateRunner = updateRunnerGO.AddComponent<UpdateRunner>();

            var lifeTimeScopeGO = new GameObject($"LifeTimeScope - {name}");
            scope = lifeTimeScopeGO.AddComponent<ConnectionManagementTestsLifeTimeScope>();
            scope.NetworkManager = networkManager;
            scope.ConnectionManager = connectionManager;
            scope.UpdateRunner = updateRunner;
            scope.Build();
        }

        void CreatePlayerPrefab()
        {
            // Create playerPrefab
            m_PlayerPrefab = new GameObject("Player");
            NetworkObject networkObject = m_PlayerPrefab.AddComponent<NetworkObject>();

            // Make it a prefab
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);

            // Set the player prefab for the server and clients
            m_ServerNetworkManager.NetworkConfig.PlayerPrefab = m_PlayerPrefab;

            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            }
        }

        protected override void OnServerAndClientsCreated()
        {
            var sceneLoaderWrapperGO = new GameObject("SceneLoader");
            sceneLoaderWrapperGO.AddComponent<SceneLoaderWrapperStub>();

            m_ClientScopes = new ConnectionManagementTestsLifeTimeScope[NumberOfClients];
            m_ClientConnectionManagers = new ConnectionManager[NumberOfClients];
            for (var i = 0; i < NumberOfClients; i++)
            {
                var clientId = i;
                InitializeInstance($"Client{clientId}", m_ClientNetworkManagers[clientId], out m_ClientConnectionManagers[clientId], out m_ClientScopes[clientId]);
            }

            InitializeInstance("Server", m_ServerNetworkManager, out m_ServerConnectionManager, out m_ServerScope);

            CreatePlayerPrefab();

            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnTearDown()
        {
            m_ServerConnectionManager.RequestShutdown();

            yield return new WaitWhile(() => m_ServerNetworkManager.IsListening);

            for (var i = 0; i < NumberOfClients; i++)
            {
                var clientId = i;
                m_ClientConnectionManagers[clientId].RequestShutdown();
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

        void AssertHostIsListening()
        {
            Assert.IsTrue(m_ServerNetworkManager.IsHost && m_ServerNetworkManager.IsListening, "Host not listening.");
        }

        void AssertAllClientsAreConnected()
        {
            for (var i = 0; i < NumberOfClients; i++)
            {
                Assert.IsTrue(m_ClientNetworkManagers[i].IsConnectedClient, $"Client{i} is not connected.");
            }
        }

        [Test]
        public void StartHost_Success()
        {
            StartHost();
            AssertHostIsListening();
        }

        [UnityTest]
        public IEnumerator StartHostAndConnectClients_Success()
        {
            StartHost();
            AssertHostIsListening();

            SetUniqueProfilesForEachClient();

            yield return ConnectClients();
            AssertAllClientsAreConnected();
        }

        [UnityTest]
        public IEnumerator UserRequestedHostShutdownAfterClientsConnected_ClientsDisconnectedWithReason()
        {
            StartHost();
            AssertHostIsListening();

            SetUniqueProfilesForEachClient();

            yield return ConnectClients();
            AssertAllClientsAreConnected();

            var nbHostEndedSessionMsgsReceived = 0;
            var subscriptions = new DisposableGroup();

            for (int i = 0; i < NumberOfClients; i++)
            {
                m_ClientScopes[i].Container.Resolve<ISubscriber<ConnectStatus>>().Subscribe(message =>
                {
                    // ignoring the first success message that is in the buffer
                    if (message != ConnectStatus.Success)
                    {
                        Assert.AreEqual(ConnectStatus.HostEndedSession, message, "Received unexpected ConnectStatus message.");
                        nbHostEndedSessionMsgsReceived++;
                    }
                });
            }

            m_ServerConnectionManager.RequestShutdown();

            yield return new WaitWhile(() => m_ServerNetworkManager.IsListening);

            Assert.IsFalse(m_ServerNetworkManager.IsHost && m_ServerNetworkManager.IsListening, "Host has not properly shut down.");

            for (var i = 0; i < NumberOfClients; i++)
            {
                var clientId = i;
                yield return new WaitWhile(() => m_ClientNetworkManagers[clientId].IsListening);
                Assert.IsFalse(m_ClientNetworkManagers[clientId].IsConnectedClient);
            }

            Assert.AreEqual(NumberOfClients, nbHostEndedSessionMsgsReceived, "Not all clients received a HostEndedSession message.");
            subscriptions.Dispose();
        }

        [UnityTest]
        public IEnumerator AttemptingToConnectWithSamePlayerId_ClientsDisconnectedWithReason()
        {
            StartHost();
            AssertHostIsListening();

            var nbLoggedInAgainMsgsReceived = 0;
            var subscriptions = new DisposableGroup();

            // setting the same profile for all clients so that they have the same player ID
            for (var i = 0; i < NumberOfClients; i++)
            {
                var profileManager = m_ClientScopes[i].Container.Resolve<ProfileManager>();
                profileManager.Profile = $"Client";
                if (i > 0)
                {
                    subscriptions.Add(m_ClientScopes[i].Container.Resolve<ISubscriber<ConnectStatus>>().Subscribe(message =>
                    {
                        Assert.AreEqual(ConnectStatus.LoggedInAgain, message, "Received unexpected ConnectStatus message.");
                        nbLoggedInAgainMsgsReceived++;
                    }));
                }
            }

            yield return ConnectClients();

            // The first client should be able to connect
            Assert.IsTrue(m_ClientNetworkManagers[0].IsConnectedClient, "The first client is not connected.");

            // Every other client should get their connection denied
            for (var i = 1; i < NumberOfClients; i++)
            {
                Assert.IsFalse(m_ClientNetworkManagers[i].IsConnectedClient, "A client with the same player ID has connected.");
            }

            Assert.AreEqual(NumberOfClients - 1, nbLoggedInAgainMsgsReceived, "Not all clients received a LoggedInAgain message.");
            subscriptions.Dispose();
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
                Assert.IsFalse(m_ClientNetworkManagers[i].IsConnectedClient, $"Client{i} is connected while no server is running.");
            }
        }

        [UnityTest]
        public IEnumerator UnexpectedClientDisconnect_ClientReconnectingSuccessfully()
        {
            StartHost();
            AssertHostIsListening();

            SetUniqueProfilesForEachClient();

            yield return ConnectClients();
            AssertAllClientsAreConnected();

            // wait for client synchronization to be over server-side before shutting down client
            yield return null;
            yield return null;

            var nbReconnectingMsgsReceived = 0;
            var subscriptions = new DisposableGroup();

            subscriptions.Add(m_ClientScopes[0].Container.Resolve<ISubscriber<ConnectStatus>>().Subscribe(message =>
            {
                // ignoring the first success message that is in the buffer
                if (message != ConnectStatus.Success)
                {
                    Assert.AreEqual(ConnectStatus.Reconnecting, message, "Received unexpected ConnectStatus message.");
                    nbReconnectingMsgsReceived++;
                }
            }));

            // Disconnecting the client at the transport level
            m_ClientNetworkManagers[0].NetworkConfig.NetworkTransport.DisconnectLocalClient();

            // Waiting until shutdown is complete
            yield return new WaitWhile(() => m_ClientNetworkManagers[0].ShutdownInProgress);
            Assert.IsFalse(m_ClientNetworkManagers[0].IsConnectedClient, "Client0 has not shut down properly.");

            // Waiting for client to automatically reconnect
            yield return WaitForClientsConnectedOrTimeOut();
            Assert.IsTrue(m_ClientNetworkManagers[0].IsConnectedClient, "Client0 failed to reconnect.");

            Assert.AreEqual(1, nbReconnectingMsgsReceived, "No Reconnecting message received.");
            subscriptions.Dispose();
        }

#if !NETCODEFORGAMEOBJECTS_1_5_2_OR_1_6_0
        [UnityTest]
        public IEnumerator UnexpectedServerShutdown_ClientsFailToReconnect()
        {
            StartHost();
            AssertHostIsListening();

            SetUniqueProfilesForEachClient();

            yield return ConnectClients();
            AssertAllClientsAreConnected();

            var nbReconnectingMsgsReceived = 0;
            var nbGenericDisconnectMsgReceived = 0;
            var subscriptions = new DisposableGroup();

            for (int i = 0; i < NumberOfClients; i++)
            {
                subscriptions.Add(m_ClientScopes[i].Container.Resolve<ISubscriber<ConnectStatus>>().Subscribe(message =>
                {
                    // ignoring the first success message that is in the buffer
                    if (message != ConnectStatus.Success)
                    {
                        var possibleMessages = new List<ConnectStatus>();
                        possibleMessages.Add(ConnectStatus.Reconnecting);
                        possibleMessages.Add(ConnectStatus.GenericDisconnect);
                        Assert.Contains(message, possibleMessages, "Received unexpected ConnectStatus message.");
                        if (message == ConnectStatus.Reconnecting)
                        {
                            nbReconnectingMsgsReceived++;
                        }
                        else if (message == ConnectStatus.GenericDisconnect)
                        {
                            nbGenericDisconnectMsgReceived++;
                        }
                    }
                }));
            }

            // Shutting down the server
            m_ServerNetworkManager.Shutdown();
            yield return new WaitWhile(() => m_ServerNetworkManager.ShutdownInProgress);
            Assert.IsFalse(m_ServerNetworkManager.IsListening, "Server has not shut down properly.");

            // Waiting until shutdown is complete for the clients as well
            for (var i = 0; i < NumberOfClients; i++)
            {
                var clientId = i;
                yield return new WaitWhile(() => m_ClientNetworkManagers[clientId].ShutdownInProgress);
                Assert.IsFalse(m_ClientNetworkManagers[clientId].IsConnectedClient, $"Client{clientId} has not shut down properly after losing connection.");
            }

            var maxNbReconnectionAttempts = 0;

            for (var i = 0; i < NumberOfClients; i++)
            {
                var nbReconnectionAttempts = m_ClientConnectionManagers[i].NbReconnectAttempts;
                maxNbReconnectionAttempts = Math.Max(maxNbReconnectionAttempts, nbReconnectionAttempts);
                for (var j = 0; j < nbReconnectionAttempts; j++)
                {
                    // Expecting this error for each reconnection attempt for each client
                    LogAssert.Expect(LogType.Error, k_FailedToConnectToServerErrorMessage);
                }
            }

            // Waiting for clients to fail to automatically reconnect. We wait once for each reconnection attempt.
            for (var i = 0; i < maxNbReconnectionAttempts; i++)
            {
                yield return WaitForClientsConnectedOrTimeOut();
                for (var j = 0; j < NumberOfClients; j++)
                {
                    Assert.IsFalse(m_ClientNetworkManagers[j].IsConnectedClient, $"Client{j} is connected while no server is running.");
                }

            }

            Assert.AreEqual(NumberOfClients, nbReconnectingMsgsReceived, "Not all clients received a Reconnecting message.");
            Assert.AreEqual(NumberOfClients, nbGenericDisconnectMsgReceived, "Not all clients received a GenericDisconnect message.");
            subscriptions.Dispose();
        }
#endif

        [UnityTest]
        public IEnumerator ClientAndHostChangingRolesBetweenSessions_Success()
        {
            SetUniqueProfilesForEachClient();

            StartHost();
            AssertHostIsListening();

            yield return ConnectClients();
            AssertAllClientsAreConnected();

            // Requesting shutdown on the server
            m_ServerConnectionManager.RequestShutdown();
            yield return new WaitWhile(() => m_ServerNetworkManager.IsListening);
            Assert.IsFalse(m_ServerNetworkManager.IsHost);

            // Waiting until shutdown is complete for the clients
            for (var i = 0; i < NumberOfClients; i++)
            {
                var clientId = i;
                yield return new WaitWhile(() => m_ClientNetworkManagers[clientId].ShutdownInProgress);
                Assert.IsFalse(m_ClientNetworkManagers[clientId].IsConnectedClient, $"Client{clientId} has not shut down properly after losing connection.");
            }

            // Switching references for Client0 and Server temporarily
            (m_ServerNetworkManager, m_ClientNetworkManagers[0]) = (m_ClientNetworkManagers[0], m_ServerNetworkManager);
            (m_ServerConnectionManager, m_ClientConnectionManagers[0]) = (m_ClientConnectionManagers[0], m_ServerConnectionManager);

            // recreate player prefab here since the GameObject has been destroyed
            CreatePlayerPrefab();

            StartHost();
            AssertHostIsListening();

            yield return ConnectClients();
            AssertAllClientsAreConnected();
        }

        [UnityTest]
        public IEnumerator ClientCancellingWhileConnectingToListeningServer_ConnectionCancelled()
        {
            StartHost();
            AssertHostIsListening();

            SetUniqueProfilesForEachClient();

            for (var i = 0; i < NumberOfClients; i++)
            {
                m_ClientConnectionManagers[i].StartClientIp($"client{i}", "127.0.0.1", 9998);
            }

            m_ClientConnectionManagers[0].RequestShutdown();

            yield return WaitForClientsConnectedOrTimeOut();

            Assert.IsFalse(m_ClientNetworkManagers[0].IsConnectedClient, "Client0 has not successfully cancelled its connection.");

            for (var i = 1; i < NumberOfClients; i++)
            {
                Assert.IsTrue(m_ClientNetworkManagers[i].IsConnectedClient, $"Client{i} is not connected.");
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
                Assert.IsFalse(m_ClientNetworkManagers[i].IsConnectedClient, $"Client{i} is connected while no server is running.");
            }
        }
    }
}
