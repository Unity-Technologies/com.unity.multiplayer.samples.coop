using System;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.BossRoom.Gameplay.UI;
using Unity.BossRoom.UnityServices.Auth;
using Unity.BossRoom.UnityServices.Sessions;
using Unity.BossRoom.Utils;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace Unity.BossRoom.Gameplay.GameState
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
        public override GameState ActiveState => GameState.MainMenu;

        [SerializeField]
        NameGenerationData m_NameGenerationData;
        [SerializeField]
        SessionUIMediator m_SessionUIMediator;
        [SerializeField]
        IPUIMediator m_IPUIMediator;
        [SerializeField]
        Button m_SessionButton;
        [SerializeField]
        GameObject m_SignInSpinner;
        [SerializeField]
        UIProfileSelector m_UIProfileSelector;
        [SerializeField]
        UITooltipDetector m_UGSSetupTooltipDetector;

        [Inject]
        AuthenticationServiceFacade m_AuthServiceFacade;
        [Inject]
        LocalSessionUser m_LocalUser;
        [Inject]
        LocalSession m_LocalSession;
        [Inject]
        ProfileManager m_ProfileManager;

        protected override void Awake()
        {
            base.Awake();

            m_SessionButton.interactable = false;
            m_SessionUIMediator.Hide();

            if (string.IsNullOrEmpty(Application.cloudProjectId))
            {
                OnSignInFailed();
                return;
            }

            TrySignIn();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.RegisterComponent(m_NameGenerationData);
            builder.RegisterComponent(m_SessionUIMediator);
            builder.RegisterComponent(m_IPUIMediator);
        }

        async void TrySignIn()
        {
            try
            {
                var unityAuthenticationInitOptions =
                    m_AuthServiceFacade.GenerateAuthenticationOptions(m_ProfileManager.Profile);

                await m_AuthServiceFacade.InitializeAndSignInAsync(unityAuthenticationInitOptions);
                OnAuthSignIn();
                m_ProfileManager.onProfileChanged += OnProfileChanged;
            }
            catch (Exception)
            {
                OnSignInFailed();
            }
        }

        void OnAuthSignIn()
        {
            m_SessionButton.interactable = true;
            m_UGSSetupTooltipDetector.enabled = false;
            m_SignInSpinner.SetActive(false);

            Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");

            m_LocalUser.ID = AuthenticationService.Instance.PlayerId;

            // The local SessionUser object will be hooked into UI before the LocalSession is populated during session join, so the LocalSession must know about it already when that happens.
            m_LocalSession.AddUser(m_LocalUser);
        }

        void OnSignInFailed()
        {
            if (m_SessionButton)
            {
                m_SessionButton.interactable = false;
                m_UGSSetupTooltipDetector.enabled = true;
            }

            if (m_SignInSpinner)
            {
                m_SignInSpinner.SetActive(false);
            }
        }

        protected override void OnDestroy()
        {
            m_ProfileManager.onProfileChanged -= OnProfileChanged;
            base.OnDestroy();
        }

        async void OnProfileChanged()
        {
            m_SessionButton.interactable = false;
            m_SignInSpinner.SetActive(true);
            await m_AuthServiceFacade.SwitchProfileAndReSignInAsync(m_ProfileManager.Profile);

            m_SessionButton.interactable = true;
            m_SignInSpinner.SetActive(false);

            Debug.Log($"Signed in. Unity Player ID {AuthenticationService.Instance.PlayerId}");

            // Updating LocalUser and LocalSession
            m_LocalSession.RemoveUser(m_LocalUser);
            m_LocalUser.ID = AuthenticationService.Instance.PlayerId;
            m_LocalSession.AddUser(m_LocalUser);
        }

        public void OnStartClicked()
        {
            m_SessionUIMediator.ToggleJoinSessionUI();
            m_SessionUIMediator.Show();
        }

        public void OnDirectIPClicked()
        {
            m_SessionUIMediator.Hide();
            m_IPUIMediator.Show();
        }

        public void OnChangeProfileClicked()
        {
            m_UIProfileSelector.Show();
        }
    }
}
