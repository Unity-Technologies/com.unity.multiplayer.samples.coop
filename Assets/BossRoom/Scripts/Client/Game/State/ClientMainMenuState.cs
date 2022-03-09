using BossRoom.Scripts.Shared.Net.UnityServices.Auth;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Services.Authentication;
using Unity.Services.Core;
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
        public override GameState ActiveState { get { return GameState.MainMenu; } }

        [SerializeField] GameObject[] m_GameObjectsThatWillBeInjectedAutomatically;
        DIScope m_Scope;

        [SerializeField] NameGenerationData m_NameGenerationData;
        [SerializeField] LobbyUIMediator m_LobbyUIMediator;

        [SerializeField] CanvasGroup m_MainMenuButtonsCanvasGroup;
        [SerializeField] GameObject m_SignInSpinner;

        void Awake()
        {
            m_MainMenuButtonsCanvasGroup.interactable = false;
            m_LobbyUIMediator.Hide();
            DIScope.RootScope.InjectIn(this);
        }

        [Inject]
        void InjectDependenciesAndInitialize(AuthenticationServiceFacade authServiceFacade, LocalLobbyUser localUser, LocalLobby localLobby)
        {
            m_Scope = new DIScope(DIScope.RootScope);

            m_Scope.BindInstanceAsSingle(m_NameGenerationData);
            m_Scope.BindInstanceAsSingle(m_LobbyUIMediator);

            var unityAuthenticationInitOptions = new InitializationOptions();
            var profile = ProfileManager.Profile;
            if (profile.Length > 0)
            {
                unityAuthenticationInitOptions.SetProfile(profile);
            }

            authServiceFacade.DoSignInAsync(OnAuthSignIn,  OnSignInFailed, unityAuthenticationInitOptions);

            m_Scope.FinalizeScopeConstruction();

            foreach (var autoInjectedGameObject in m_GameObjectsThatWillBeInjectedAutomatically)
            {
                m_Scope.InjectIn(autoInjectedGameObject);
            }

            void OnAuthSignIn()
            {
                m_MainMenuButtonsCanvasGroup.interactable = true;
                m_SignInSpinner.SetActive(false);

                Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");

                localUser.ID = AuthenticationService.Instance.PlayerId;
                // The local LobbyUser object will be hooked into UI before the LocalLobby is populated during lobby join, so the LocalLobby must know about it already when that happens.
                localLobby.AddUser(localUser);
            }

            void OnSignInFailed()
            {
                m_SignInSpinner.SetActive(false);
                PopupPanel.ShowPopupPanel("Authentication Error", "For some reason we can't authenticate the user anonymously - that typically means that project is not properly set up with Unity services.");
            }
        }

        public override void OnDestroy()
        {
            m_Scope?.Dispose();
        }

        public void OnStartClicked()
        {
            m_LobbyUIMediator.ToggleJoinLobbyUI();
            m_LobbyUIMediator.Show();
        }
    }
}
