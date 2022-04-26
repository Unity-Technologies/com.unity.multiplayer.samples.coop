using System;
using BossRoom.Scripts.Shared.Net.UnityServices.Auth;
using Unity.Multiplayer.Samples.BossRoom.Shared;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;

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

        [SerializeField] NameGenerationData m_NameGenerationData;
        [SerializeField] LobbyUIMediator m_LobbyUIMediator;
        [SerializeField] IPUIMediator m_IPUIMediator;
        [SerializeField] Button m_LobbyButton;
        [SerializeField] GameObject m_SignInSpinner;

        protected override void Awake()
        {
            m_LobbyButton.interactable = false;
            m_LobbyUIMediator.Hide();
            base.Awake();
        }

        protected override void InitializeScope()
        {
            Scope.BindInstanceAsSingle(m_NameGenerationData);
            Scope.BindInstanceAsSingle(m_LobbyUIMediator);
            Scope.BindInstanceAsSingle(m_IPUIMediator);
        }

        [Inject]
        async void InjectDependenciesAndInitialize(AuthenticationServiceFacade authServiceFacade, LocalLobbyUser localUser, LocalLobby localLobby)
        {
            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                PopupManager.ShowPopupPanel("Unity Gaming Services ProjectID not set up" ,"Click the Readme file in the Assets Folder within the Project window in-editor to follow \"How to set up Unity Gaming Services\"");
                OnSignInFailed();
                return;
            }

            try
            {
                var unityAuthenticationInitOptions = new InitializationOptions();
                var profile = ProfileManager.Profile;
                if (profile.Length > 0)
                {
                    unityAuthenticationInitOptions.SetProfile(profile);
                }

                await authServiceFacade.InitializeAndSignInAsync(unityAuthenticationInitOptions);
                OnAuthSignIn();
            }
            catch (Exception)
            {
                OnSignInFailed();
            }

            void OnAuthSignIn()
            {
                m_LobbyButton.interactable = true;
                m_SignInSpinner.SetActive(false);

                Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");

                localUser.ID = AuthenticationService.Instance.PlayerId;
                // The local LobbyUser object will be hooked into UI before the LocalLobby is populated during lobby join, so the LocalLobby must know about it already when that happens.
                localLobby.AddUser(localUser);
            }

            void OnSignInFailed()
            {
                m_LobbyButton.interactable = false;
                m_SignInSpinner.SetActive(false);
            }
        }

        public void OnStartClicked()
        {
            m_LobbyUIMediator.ToggleJoinLobbyUI();
            m_LobbyUIMediator.Show();
        }

        public void OnDirectIPClicked()
        {
            m_LobbyUIMediator.Hide();
            m_IPUIMediator.Show();
        }
    }
}
