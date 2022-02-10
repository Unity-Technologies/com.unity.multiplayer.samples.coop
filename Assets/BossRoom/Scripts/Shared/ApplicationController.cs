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
    /// <summary>
    /// An entry point to the application, where we bind all the common dependencies to the root DI scope.
    /// </summary>
    public class ApplicationController : MonoBehaviour
    {
        [SerializeField] private UpdateRunner m_UpdateRunner;
        [SerializeField] private GameNetPortal m_GameNetPortal;
        [SerializeField] private ClientGameNetPortal m_ClientNetPortal;

        private LocalLobby m_LocalLobby;
        private LobbyUser m_LocalUser;
        private LobbyAsyncRequests m_LobbyAsyncRequests;
        private LobbyContentHeartbeat m_LobbyContentHeartbeat;

        [SerializeField] private GameObject[] m_AutoInjected;

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
            scope.BindAsSingle<LobbyUser>();
            scope.BindAsSingle<LocalLobby>();

            //this message channel is essential and persists for the lifetime of the lobby and relay services
            scope.BindMessageChannel<UnityServiceErrorMessage>();

            //all the lobby service stuff, bound here so that it persists through scene loads
            scope.BindAsSingle<AuthenticationAPIInterface>(); //a manager entity that allows us to do anonymous authentication with unity services
            scope.BindAsSingle<LobbyServiceData>();
            scope.BindAsSingle<LobbyContentHeartbeat>();
            scope.BindAsSingle<LobbyAPIInterface>();
            scope.BindAsSingle<LobbyAsyncRequests>();


            scope.FinalizeScopeConstruction();

            foreach (var o in m_AutoInjected)
            {
                scope.InjectIn(o);
            }

            m_LocalLobby = scope.Resolve<LocalLobby>();
            m_LobbyAsyncRequests = scope.Resolve<LobbyAsyncRequests>();
            m_LobbyContentHeartbeat = scope.Resolve<LobbyContentHeartbeat>();
            m_LocalUser = scope.Resolve<LobbyUser>();
        }

        private void Start()
        {
            SceneManager.LoadScene("MainMenu");
        }

        private void OnDestroy()
        {
            ForceLeaveLobbyAttempt();
            DIScope.RootScope.Dispose();
        }

        /// <summary>
        ///     In builds, if we are in a lobby and try to send a Leave request on application quit, it won't go through if we're quitting on the same frame.
        ///     So, we need to delay just briefly to let the request happen (though we don't need to wait for the result).
        /// </summary>
        private IEnumerator LeaveBeforeQuit()
        {
            ForceLeaveLobbyAttempt();
            yield return null;
            Application.Quit();
        }

        private bool OnWantToQuit()
        {
            var canQuit = string.IsNullOrEmpty(m_LocalLobby?.LobbyID);
            StartCoroutine(LeaveBeforeQuit());
            return canQuit;
        }

        private void ForceLeaveLobbyAttempt()
        {
            m_LobbyAsyncRequests.EndTracking();
            m_LobbyContentHeartbeat.EndTracking();

            if (!string.IsNullOrEmpty(m_LocalLobby?.LobbyID))
            {
                m_LobbyAsyncRequests.LeaveLobbyAsync(m_LocalLobby?.LobbyID, null, null);
            }

            m_LocalUser.ResetState();
            m_LocalLobby.Reset(m_LocalUser);
        }

        public void QuitGame()
        {
            ForceLeaveLobbyAttempt();

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
