using BossRoom.Scripts.Client.UI;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Auth;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Game Logic that runs when sitting at the MainMenu. This is likely to be "nothing", as no game has been started. But it is
    /// nonetheless important to have a game state, as the GameStateBehaviour system requires that all scenes have states.
    /// </summary>
    /// <remarks> OnNetworkSpawn() won't ever run, because there is no network connection at the main menu screen.
    /// Fortunately we know you are a client, because all players are clients when sitting at the main menu screen.
    /// </remarks>
    public class ClientMainMenuState : GameStateBehaviour
    {
        public override GameState ActiveState { get { return GameState.MainMenu;  } }

        private GameNetPortal m_GameNetPortal;

        private ClientGameNetPortal m_ClientNetPortal;


        [SerializeField] private GameObject[] _autoInjected;
        private DIScope m_Scope;

        [SerializeField] private NameGenerationData m_NameGenerationData;
        [SerializeField] private LobbyUIMediator m_lobbyUIMediator;

        [SerializeField] private CanvasGroup m_mainMenuButtons;
        [SerializeField] private GameObject m_signInSpinner;

        private void Awake()
        {
            m_mainMenuButtons.interactable = false;
            m_lobbyUIMediator.Hide();
            DIScope.RootScope.InjectIn(this);
        }

        [Inject]
        private void InjectDependenciesAndInitialize(Identity identity)
        {
            m_Scope = new DIScope(DIScope.RootScope);

            m_Scope.BindInstanceAsSingle(m_NameGenerationData);
            m_Scope.BindInstanceAsSingle(m_lobbyUIMediator);
            m_Scope.BindAsSingle<GameObjectFactory>();

            identity.DoAuthSignIn(OnAuthSignIn);

            void OnAuthSignIn()
            {
                Debug.Log("Signed in.");

                m_Scope.FinalizeScopeConstruction();

                foreach (var go in _autoInjected)
                {
                    m_Scope.InjectIn(go);
                }

                m_mainMenuButtons.interactable = true;
                m_signInSpinner.SetActive(false);

                var localUser = m_Scope.Resolve<LobbyUser>();
                var identity = m_Scope.Resolve<Identity>();
                var localLobby = m_Scope.Resolve<LocalLobby>();

                localUser.ID = identity.GetSubIdentity(IIdentityType.Auth).GetContent("id");
                localUser.DisplayName = m_NameGenerationData.GenerateName();
                // The local LobbyUser object will be hooked into UI before the LocalLobby is populated during lobby join, so the LocalLobby must know about it already when that happens.
                localLobby.AddPlayer(localUser);
            }
        }

        public override void OnDestroy()
        {
            m_Scope?.Dispose();
        }

        public void OnStartClicked()
        {
            m_lobbyUIMediator.ToggleJoinLobbyUI();
            m_lobbyUIMediator.Show();
        }
    }
}
