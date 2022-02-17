using System.Collections;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Auth;
using BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossRoom.Scripts.Shared
{

    public struct ApplicationIsQuittingMessage
    {

    }

    /// <summary>
    /// An entry point to the application, where we bind all the common dependencies to the root DI scope.
    /// </summary>
    public class ApplicationController : MonoBehaviour
    {
        [SerializeField] private UpdateRunner m_UpdateRunner;
        [SerializeField] private GameNetPortal m_GameNetPortal;
        [SerializeField] private ClientGameNetPortal m_ClientNetPortal;

        private LocalLobby m_LocalLobby;
        private LobbyServiceFacade m_LobbyServiceFacade;

        [SerializeField] private GameObject[] m_GameObjectsThatWillBeInjectedAutomatically;

        private void Awake()
        {
            Application.wantsToQuit += OnWantToQuit;

            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(m_UpdateRunner.gameObject);

            var scope = DIScope.RootScope;

            scope.BindInstanceAsSingle(this);
            scope.BindInstanceAsSingle(m_UpdateRunner);
            scope.BindInstanceAsSingle(m_GameNetPortal);
            scope.BindInstanceAsSingle(m_ClientNetPortal);

            //the following singletons represent the local representations of the lobby that we're in and the user that we are
            //they can persist longer than the lifetime of the UI in MainMenu where we set up the lobby that we create or join
            scope.BindAsSingle<LocalLobbyUser>();
            scope.BindAsSingle<LocalLobby>();

            //this message channel is essential and persists for the lifetime of the lobby and relay services
            scope.BindMessageChannel<UnityServiceErrorMessage>();

            //buffered message channels hold the latest received message in buffer and pass to any new subscribers
            scope.BindBufferedMessageChannel<LobbyListFetchedMessage>();

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
        }

        private void Start()
        {
            SceneManager.LoadScene("MainMenu");
        }

        private void OnDestroy()
        {
            m_LobbyServiceFacade.ForceLeaveLobbyAttempt();
            DIScope.RootScope.Dispose();
        }

        /// <summary>
        ///     In builds, if we are in a lobby and try to send a Leave request on application quit, it won't go through if we're quitting on the same frame.
        ///     So, we need to delay just briefly to let the request happen (though we don't need to wait for the result).
        /// </summary>
        private IEnumerator LeaveBeforeQuit()
        {
            m_LobbyServiceFacade.ForceLeaveLobbyAttempt();
            yield return null;
            Application.Quit();
        }

        private bool OnWantToQuit()
        {
            var canQuit = string.IsNullOrEmpty(m_LocalLobby?.LobbyID);
            StartCoroutine(LeaveBeforeQuit());
            return canQuit;
        }

        public void QuitGame()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                // first disconnect then return to menu
                var gameNetPortal = GameNetPortal.Instance;
                if (gameNetPortal != null)
                {
                    gameNetPortal.RequestDisconnect();
                }
                SceneManager.LoadScene("MainMenu");
            }
            else
            {
                Application.Quit();
            }
        }
    }
}
