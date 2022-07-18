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
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;
using VContainer.Unity;

namespace Unity.Multiplayer.Samples.BossRoom.Tests.Runtime
{
    public class ConnectionManagementTests : NetcodeIntegrationTest
    {
        class ConnectionManagementTestsLifeTimeScope : LifetimeScope
        {
            public NetworkManager NetworkManager;
            public ConnectionManager ConnectionManager;
            public UpdateRunner UpdateRunner;

            protected override void Awake() { }

            protected override void Configure(IContainerBuilder builder)
            {
                NetworkManager.NetworkConfig.ConnectionApproval = true;
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

        protected override void OnServerAndClientsCreated()
        {
            var sceneLoaderWrapperGO = new GameObject("SceneLoader");
            sceneLoaderWrapperGO.AddComponent<SceneLoaderWrapperStub>();

            m_ClientScopes = new ConnectionManagementTestsLifeTimeScope[NumberOfClients];
            m_ClientConnectionManagers = new ConnectionManager[NumberOfClients];
            m_ClientConnectionStateSequences = new List<ConnectionState>[NumberOfClients];
            for (var i = 0; i < NumberOfClients; i++)
            {
                var clientId = i;
                var clientConnectionManagerGO = new GameObject("ConnectionManager - Client - " + clientId);
                m_ClientConnectionManagers[clientId] = clientConnectionManagerGO.AddComponent<ConnectionManager>();

                var clientUpdateRunnerGO = new GameObject("UpdateRunner - Client - " + clientId);
                var clientUpdateRunner = clientUpdateRunnerGO.AddComponent<UpdateRunner>();

                var clientLifeTimeScopeGO = new GameObject("LifeTimeScope - Client - " + clientId);
                m_ClientScopes[clientId] = clientLifeTimeScopeGO.AddComponent<ConnectionManagementTestsLifeTimeScope>();
                m_ClientScopes[clientId].NetworkManager = m_ClientNetworkManagers[clientId];
                m_ClientScopes[clientId].ConnectionManager = m_ClientConnectionManagers[clientId];
                m_ClientScopes[clientId].UpdateRunner = clientUpdateRunner;
                m_ClientScopes[clientId].Build();

                m_ClientConnectionStateSequences[clientId] = new List<ConnectionState>();
                m_ClientConnectionManagers[clientId].m_OnStateChanged += state => m_ClientConnectionStateSequences[clientId].Add(state);
            }

            // Create gameObject
            var serverConnectionManagerGO = new GameObject("ConnectionManager - Server");
            // Create networkManager component
            m_ServerConnectionManager = serverConnectionManagerGO.AddComponent<ConnectionManager>();

            var serverUpdateRunnerGO = new GameObject("UpdateRunner - Server");
            var serverUpdateRunner = serverUpdateRunnerGO.AddComponent<UpdateRunner>();

            var serverLifeTimeScopeGO = new GameObject("LifeTimeScope - Server");
            m_ServerScope = serverLifeTimeScopeGO.AddComponent<ConnectionManagementTestsLifeTimeScope>();
            m_ServerScope.NetworkManager = m_ServerNetworkManager;
            m_ServerScope.ConnectionManager = m_ServerConnectionManager;
            m_ServerScope.UpdateRunner = serverUpdateRunner;
            m_ServerScope.Build();

            m_ServerConnectionStateSequence = new List<ConnectionState>();
            m_ServerConnectionManager.m_OnStateChanged += state => m_ServerConnectionStateSequence.Add(state);

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

        IEnumerator StartHost()
        {
            m_ServerConnectionManager.StartHostIp("server", "127.0.0.1", 9998);
            yield return null;
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

        [UnityTest]
        public IEnumerator StartHost_Valid()
        {
            yield return StartHost();
            Assert.IsTrue(m_ServerNetworkManager.IsHost);
            var expectedServerConnectionStateSequence = new List<ConnectionState>();
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_StartingHost);
            expectedServerConnectionStateSequence.Add(m_ServerConnectionManager.m_Hosting);
            Assert.AreEqual(expectedServerConnectionStateSequence, m_ServerConnectionStateSequence);
        }

        [UnityTest]
        public IEnumerator StartHostAndConnectClients_Valid()
        {
            yield return StartHost();
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
        public IEnumerator ClientsDisconnectedWithReasonAfterUserRequestedHostShutdown_Valid()
        {
            yield return StartHost();
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
        public IEnumerator ClientsDisconnectedWithReasonAfterAttemptingToConnectWithSamePlayerId_Valid()
        {
            yield return StartHost();
            Assert.IsTrue(m_ServerNetworkManager.IsHost);

            for (var i = 0; i < NumberOfClients; i++)
            {
                var profileManager = m_ClientScopes[i].Container.Resolve<ProfileManager>();
                profileManager.Profile = $"Client";
            }

            yield return ConnectClients();

            Assert.IsTrue(m_ClientNetworkManagers[0].IsConnectedClient);
            Assert.IsFalse(m_ClientNetworkManagers[1].IsConnectedClient);

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
        public IEnumerator ClientConnectingWithoutHost_Failed()
        {
            SetUniqueProfilesForEachClient();

            const string errorMessage = "Failed to connect to server.";
            LogAssert.Expect(LogType.Error, errorMessage);
            LogAssert.Expect(LogType.Error, errorMessage);
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

    }
}