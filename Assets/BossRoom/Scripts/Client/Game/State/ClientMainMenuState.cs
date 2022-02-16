using BossRoom.Scripts.Client.UI;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Auth;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using ParrelSync;
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
        public override GameState ActiveState { get { return GameState.MainMenu;  } }

        private GameNetPortal m_GameNetPortal;

        private ClientGameNetPortal m_ClientNetPortal;


        [SerializeField] private GameObject[] m_GameObjectsThatWillBeInjectedAutomatically;
        private DIScope m_Scope;

        [SerializeField] private NameGenerationData m_NameGenerationData;
        [SerializeField] private LobbyUIMediator m_LobbyUIMediator;

        [SerializeField] private CanvasGroup m_MainMenuButtons;
        [SerializeField] private GameObject m_SignInSpinner;

        private void Awake()
        {
            m_MainMenuButtons.interactable = false;
            m_LobbyUIMediator.Hide();
            DIScope.RootScope.InjectIn(this);
        }

        [Inject]
        private void InjectDependenciesAndInitialize(AuthenticationAPIInterface authAPIInterface, LobbyUser localUser, LocalLobby localLobby)
        {
            m_Scope = new DIScope(DIScope.RootScope);

            m_Scope.BindInstanceAsSingle(m_NameGenerationData);
            m_Scope.BindInstanceAsSingle(m_LobbyUIMediator);

            var unityAuthenticationInitOptions = new InitializationOptions();

#if UNITY_EDITOR
            //The code below makes it possible for the clone instance to log in as a different user profile in Authentication service.
            //This allows us to test services integration locally by utilising Parrelsync.
            if (ClonesManager.IsClone())
            {
                Debug.Log("This is a clone project.");
                var customArguments = ClonesManager.GetArgument().Split(',');

                //second argument is our custom ID, but if it's not set we would just use some default.

                var hardcodedProfileID = customArguments.Length > 1 ? customArguments[1] : "defaultCloneID";

                unityAuthenticationInitOptions.SetProfile(hardcodedProfileID);
            }
#endif
            authAPIInterface.DoSignInAsync(OnAuthSignIn,  OnSignInFailed, unityAuthenticationInitOptions);

            void OnAuthSignIn()
            {
                m_Scope.FinalizeScopeConstruction();

                foreach (var go in m_GameObjectsThatWillBeInjectedAutomatically)
                {
                    m_Scope.InjectIn(go);
                }

                m_MainMenuButtons.interactable = true;
                m_SignInSpinner.SetActive(false);

                Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");

                localUser.ID = AuthenticationService.Instance.PlayerId;
                localUser.DisplayName = m_NameGenerationData.GenerateName();
                // The local LobbyUser object will be hooked into UI before the LocalLobby is populated during lobby join, so the LocalLobby must know about it already when that happens.
                localLobby.AddPlayer(localUser);
            }

            void OnSignInFailed()
            {
                Debug.LogError("For some reason we can't authenticate the user anonymously - that typically means that project is not properly set up with Unity services.");
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
