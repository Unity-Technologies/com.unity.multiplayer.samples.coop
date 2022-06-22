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

namespace Unity.Multiplayer.Samples.BossRoom.Shared
{
    /// <summary>
    /// An entry point to the application, where we bind all the common dependencies to the root DI scope.
    /// </summary>
    public class ApplicationController : MonoBehaviour
    {
        [SerializeField] UpdateRunner m_UpdateRunner;
        [SerializeField] ConnectionManager m_ConnectionManager;

        LocalLobby m_LocalLobby;
        LobbyServiceFacade m_LobbyServiceFacade;
        IDisposable m_Subscriptions;

        [SerializeField] GameObject[] m_GameObjectsThatWillBeInjectedAutomatically;

        private void Awake()
        {
            Application.wantsToQuit += OnWantToQuit;

            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(m_UpdateRunner.gameObject);

            var scope = DIScope.RootScope;

            scope.BindInstanceAsSingle(this);
            scope.BindInstanceAsSingle(m_UpdateRunner);
            scope.BindInstanceAsSingle(m_ConnectionManager);

            //the following singletons represent the local representations of the lobby that we're in and the user that we are
            //they can persist longer than the lifetime of the UI in MainMenu where we set up the lobby that we create or join
            scope.BindAsSingle<LocalLobbyUser>();
            scope.BindAsSingle<LocalLobby>();

            scope.BindAsSingle<ProfileManager>();

            //these message channels are essential and persist for the lifetime of the lobby and relay services
            scope.BindMessageChannelInstance<QuitApplicationMessage>();
            scope.BindMessageChannelInstance<UnityServiceErrorMessage>();
            scope.BindBufferedMessageChannelInstance<ConnectStatus>();
            scope.BindMessageChannelInstance<DoorStateChangedEventMessage>();

            //these message channels are essential and persist for the lifetime of the lobby and relay services
            //they are networked so that the clients can subscribe to those messages that are published by the server
            scope.BindNetworkedMessageChannelInstance<LifeStateChangedEventMessage>();
            scope.BindNetworkedMessageChannelInstance<ConnectionEventMessage>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            scope.BindNetworkedMessageChannelInstance<CheatUsedMessage>();
#endif

            //this message channel is essential and persists for the lifetime of the lobby and relay services
            scope.BindMessageChannelInstance<ReconnectMessage>();

            //buffered message channels hold the latest received message in buffer and pass to any new subscribers
            scope.BindBufferedMessageChannelInstance<LobbyListFetchedMessage>();

            //all the lobby service stuff, bound here so that it persists through scene loads
            scope.BindAsSingle<AuthenticationServiceFacade>(); //a manager entity that allows us to do anonymous authentication with unity services
            scope.BindAsSingle<LobbyServiceFacade>();

            scope.FinalizeScopeConstruction();

            foreach (var o in m_GameObjectsThatWillBeInjectedAutomatically)
            {
                scope.InjectIn(o);
            }

            m_LocalLobby = scope.Resolve<LocalLobby>();
            m_LobbyServiceFacade = scope.Resolve<LobbyServiceFacade>();

            var quitApplicationSub = scope.Resolve<ISubscriber<QuitApplicationMessage>>();

            var subHandles = new DisposableGroup();
            subHandles.Add(quitApplicationSub.Subscribe(QuitGame));
            m_Subscriptions = subHandles;

            Application.targetFrameRate = 120;
        }

        private void OnDestroy()
        {
            m_Subscriptions?.Dispose();
            m_LobbyServiceFacade?.EndTracking();
            DIScope.RootScope.Dispose();
            DIScope.RootScope = null;
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
