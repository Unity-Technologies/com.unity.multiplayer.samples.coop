using System.Collections;
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
            for (var i = 0; i < NumberOfClients; i++)
            {
                var clientConnectionManagerGO = new GameObject("ConnectionManager - Client - " + i);
                m_ClientConnectionManagers[i] = clientConnectionManagerGO.AddComponent<ConnectionManager>();

                var clientUpdateRunnerGO = new GameObject("UpdateRunner - Client - " + i);
                var clientUpdateRunner = clientUpdateRunnerGO.AddComponent<UpdateRunner>();

                var clientLifeTimeScopeGO = new GameObject("LifeTimeScope - Client - " + i);
                m_ClientScopes[i] = clientLifeTimeScopeGO.AddComponent<ConnectionManagementTestsLifeTimeScope>();
                m_ClientScopes[i].NetworkManager = m_ClientNetworkManagers[i];
                m_ClientScopes[i].ConnectionManager = m_ClientConnectionManagers[i];
                m_ClientScopes[i].UpdateRunner = clientUpdateRunner;
                m_ClientScopes[i].Build();

                var profileManager = m_ClientScopes[i].Container.Resolve<ProfileManager>();
                profileManager.Profile = $"Client{i}";
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

        [UnityTest]
        public IEnumerator StartHost_Valid()
        {
            m_ServerConnectionManager.StartHostIp("server", "127.0.0.1", 9998);
            yield return null;
            Assert.IsTrue(m_ServerNetworkManager.IsServer);
        }

        [UnityTest]
        public IEnumerator StartHostAndConnectClients_Valid()
        {
            m_ServerConnectionManager.StartHostIp("server", "127.0.0.1", 9998);
            yield return null;
            Assert.IsTrue(m_ServerNetworkManager.IsHost);
            for (int i = 0; i < NumberOfClients; i++)
            {
                m_ClientConnectionManagers[i].StartClientIp($"client{i}", "127.0.0.1", 9998);
            }

            yield return WaitForClientsConnectedOrTimeOut(m_ClientNetworkManagers);
            for (int i = 0; i < NumberOfClients; i++)
            {
                UnityEngine.Assertions.Assert.IsTrue(m_ClientNetworkManagers[i].IsClient);
            }
        }

    }
}
