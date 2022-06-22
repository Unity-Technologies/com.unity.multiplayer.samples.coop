using System;
using System.Collections;
using BossRoom.Scripts.Shared.Net.UnityServices.Auth;
using Unity.Multiplayer.Samples.BossRoom.ApplicationLifecycle.Messages;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Server;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Unity.Multiplayer.Samples.BossRoom.Shared
{

    /// <summary>
    /// An entry point to the application, where we bind all the common dependencies to the root DI scope.
    /// </summary>
    public class ApplicationController : LifetimeScope
    {
        [SerializeField] UpdateRunner m_UpdateRunner;
        [SerializeField] GameNetPortal m_GameNetPortal;
        [SerializeField] ClientGameNetPortal m_ClientNetPortal;
        [SerializeField] ServerGameNetPortal m_ServerGameNetPortal;

        LocalLobby m_LocalLobby;
        LobbyServiceFacade m_LobbyServiceFacade;

        IDisposable m_Subscriptions;

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.RegisterComponent(m_UpdateRunner);
            builder.RegisterComponent(m_GameNetPortal);
            builder.RegisterComponent(m_ClientNetPortal);
            builder.RegisterComponent(m_ServerGameNetPortal);

            //the following singletons represent the local representations of the lobby that we're in and the user that we are
            //they can persist longer than the lifetime of the UI in MainMenu where we set up the lobby that we create or join
            builder.Register<LocalLobbyUser>(Lifetime.Singleton);
            builder.Register<LocalLobby>(Lifetime.Singleton);

            builder.Register<ProfileManager>(Lifetime.Singleton);

            //these message channels are essential and persist for the lifetime of the lobby and relay services
            builder.RegisterInstance(new MessageChannel<QuitGameSessionMessage>()).AsImplementedInterfaces();
            builder.RegisterInstance(new MessageChannel<QuitApplicationMessage>()).AsImplementedInterfaces();
            builder.RegisterInstance(new MessageChannel<UnityServiceErrorMessage>()).AsImplementedInterfaces();
            builder.RegisterInstance(new MessageChannel<ConnectStatus>()).AsImplementedInterfaces();
            builder.RegisterInstance(new MessageChannel<DoorStateChangedEventMessage>()).AsImplementedInterfaces();

            //these message channels are essential and persist for the lifetime of the lobby and relay services
            //they are networked so that the clients can subscribe to those messages that are published by the server
            builder.RegisterInstance(new NetworkedMessageChannel<LifeStateChangedEventMessage>()).AsImplementedInterfaces();
            builder.RegisterInstance(new NetworkedMessageChannel<ConnectionEventMessage>()).AsImplementedInterfaces();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            builder.RegisterInstance(new NetworkedMessageChannel<CheatUsedMessage>()).AsImplementedInterfaces();
#endif

            //this message channel is essential and persists for the lifetime of the lobby and relay services
            builder.RegisterInstance(new MessageChannel<ReconnectMessage>()).AsImplementedInterfaces();

            //buffered message channels hold the latest received message in buffer and pass to any new subscribers
            builder.RegisterInstance(new BufferedMessageChannel<LobbyListFetchedMessage>()).AsImplementedInterfaces();

            //all the lobby service stuff, bound here so that it persists through scene loads
            builder.Register<AuthenticationServiceFacade>(Lifetime.Singleton); //a manager entity that allows us to do anonymous authentication with unity services

            //LobbyServiceFacade is registered as entrypoint because it wants a callback after container is built to do it's initialization
            builder.RegisterEntryPoint<LobbyServiceFacade>().AsSelf();
        }

        private void Start()
        {
            m_LocalLobby = Container.Resolve<LocalLobby>();
            m_LobbyServiceFacade = Container.Resolve<LobbyServiceFacade>();

            var quitGameSessionSub = Container.Resolve<ISubscriber<QuitGameSessionMessage>>();
            var quitApplicationSub = Container.Resolve<ISubscriber<QuitApplicationMessage>>();

            var subHandles = new DisposableGroup();
            subHandles.Add(quitGameSessionSub.Subscribe(LeaveSession));
            subHandles.Add(quitApplicationSub.Subscribe(QuitGame));
            m_Subscriptions = subHandles;

            Application.wantsToQuit += OnWantToQuit;
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(m_UpdateRunner.gameObject);
            Application.targetFrameRate = 120;
            SceneManager.LoadScene("MainMenu");
        }

        protected override void OnDestroy()
        {
            m_Subscriptions?.Dispose();
            m_LobbyServiceFacade?.EndTracking();
            base.OnDestroy();
        }

        /// <summary>
        ///     In builds, if we are in a lobby and try to send a Leave request on application quit, it won't go through if we're quitting on the same frame.
        ///     So, we need to delay just briefly to let the request happen (though we don't need to wait for the result).
        /// </summary>
        private IEnumerator LeaveBeforeQuit()
        {
            // We want to quit anyways, so if anything happens while trying to leave the Lobby, log the exception then carry on
            try
            {
                m_LobbyServiceFacade.EndTracking();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            yield return null;
            Application.Quit();
        }

        private bool OnWantToQuit()
        {
            var canQuit = string.IsNullOrEmpty(m_LocalLobby?.LobbyID);
            if (!canQuit)
            {
                StartCoroutine(LeaveBeforeQuit());
            }
            return canQuit;
        }

        private void LeaveSession(QuitGameSessionMessage msg)
        {
            m_LobbyServiceFacade.EndTracking();

            if (msg.UserRequested)
            {
                // first disconnect then return to menu
                var gameNetPortal = GameNetPortal.Instance;
                if (gameNetPortal != null)
                {
                    gameNetPortal.RequestDisconnect();
                }
            }
            SceneLoaderWrapper.Instance.LoadScene("MainMenu", useNetworkSceneManager: false);
        }

        private void QuitGame(QuitApplicationMessage msg)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
