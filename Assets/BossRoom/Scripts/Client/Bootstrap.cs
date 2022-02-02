using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Auth;
using BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.BossRoom.Client;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossRoom.Scripts.Client
{
    /// <summary>
    /// An entry point to the application, where we bind all the common dependencies to the root DI scope.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private UpdateRunner m_UpdateRunner;
        [SerializeField] private GameNetPortal m_GameNetPortal;
        [SerializeField] private ClientGameNetPortal m_ClientNetPortal;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(m_UpdateRunner.gameObject);

            var scope = DIScope.RootScope;

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
            scope.BindAsSingle<LobbyServiceData>();
            scope.BindAsSingle<LobbyContentHeartbeat>();
            scope.BindAsSingle<LocalGameState>();
            scope.BindAsSingle<LobbyAPIInterface>();
            scope.BindAsSingle<LobbyAsyncRequests>();
            scope.BindAsSingle<LobbyUserFactory>(); //a factory to create injected lobby users
            scope.BindAsSingle<LocalLobbyFactory>(); //a factory to create injected local lobbies for lobbies that we query from the lobby service
            scope.BindAsSingle<Identity>(); //a manager entity that allows us to do anonymous authentication with unity services

            //todo: remember to cleanup unused message channels
            scope.BindMessageChannel<ClientUserSeekingDisapproval>();
            scope.BindMessageChannel<ClientUserApproved>();
            scope.BindMessageChannel<UserStatus>();
            scope.BindMessageChannel<StartCountdown>();
            scope.BindMessageChannel<CancelCountdown>();
            scope.BindMessageChannel<CompleteCountdown>();
            scope.BindMessageChannel<ConfirmInGameState>();
            scope.BindMessageChannel<ChangeGameState>();
            scope.BindMessageChannel<EndGame>();

            scope.FinalizeScopeConstruction();
        }

        private void Start()
        {
            SceneManager.LoadScene("MainMenu");
        }

        private void OnDestroy()
        {
            DIScope.RootScope.Dispose();
        }
    }
}
