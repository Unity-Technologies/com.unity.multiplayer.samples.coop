using BossRoom.Scripts.Client.UI;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Auth;
using BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

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
            CreateDIScope();
        }

        private void CreateDIScope()
        {
            m_Scope = new DIScope(DIScope.RootScope);

            m_Scope.BindInstanceAsSingle(m_NameGenerationData);

            m_Scope.BindMessageChannel<ClientUserSeekingDisapproval>();

            //todo: remember to cleanup unused message channels

            m_Scope.BindMessageChannel<ClientUserApproved>();
            m_Scope.BindMessageChannel<UserStatus>();
            m_Scope.BindMessageChannel<StartCountdown>();
            m_Scope.BindMessageChannel<CancelCountdown>();
            m_Scope.BindMessageChannel<CompleteCountdown>();
            m_Scope.BindMessageChannel<ConfirmInGameState>();
            m_Scope.BindMessageChannel<ChangeGameState>();
            m_Scope.BindMessageChannel<EndGame>();

            m_Scope.BindAsSingle<LobbyAPIInterface>();
            m_Scope.BindAsSingle<LobbyAsyncRequests>();

            m_Scope.BindAsSingle<LobbyUserFactory>(); //a factory to create injected lobby users

            m_Scope.BindAsSingle<LocalLobbyFactory>(); //a factory to create injected local lobbies for lobbies that we query from the lobby service
            m_Scope.BindAsSingle<GameObjectFactory>();

            //var playerNetworkObject = Netcode.NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(Netcode.NetworkManager.Singleton.LocalClientId);
           // var persistentPlayer = playerNetworkObject.GetComponent<PersistentPlayer>();
           // _container.BindInstanceAsSingle(persistentPlayer);

            m_Scope.BindInstanceAsSingle(m_lobbyUIMediator);
            m_Scope.BindInstanceAsSingle(new Identity(OnAuthSignIn));

            void OnAuthSignIn()
            {
                Debug.Log("Signed in.");

                m_Scope.FinalizeScopeConstruction();

                foreach (var go in _autoInjected)
                {
                    m_Scope.Inject(go);
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

        protected override void Start()
        {
            // Find the game Net Portal by tag - it should have been created by Startup
            GameObject GamePortalGO = GameObject.FindGameObjectWithTag("GameNetPortal");
            Assert.IsNotNull("No GameNetPortal found, Did you start the game from the Startup scene?");
            m_GameNetPortal = GamePortalGO.GetComponent<GameNetPortal>();
            m_ClientNetPortal = GamePortalGO.GetComponent<Client.ClientGameNetPortal>();

            m_ClientNetPortal.NetworkTimedOut += OnNetworkTimeout;
            m_ClientNetPortal.OnUnityRelayJoinFailed += OnRelayJoinFailed;
            m_ClientNetPortal.ConnectFinished += OnConnectFinished;

            m_lobbyUIMediator.Hide();

            //any disconnect reason set? Show it to the user here.
            ConnectStatusToMessage(m_ClientNetPortal.DisconnectReason.Reason, false);
            m_ClientNetPortal.DisconnectReason.Clear();
        }

        public void OnStartClicked()
        {
            m_lobbyUIMediator.ToggleJoinLobbyUI();
            m_lobbyUIMediator.Show();
        }

        // public void OnHostClicked()
        // {
        //     m_ResponsePopup.SetupEnterGameDisplay(true, "Host Game", "Input the Host IP <br> or select another mode", "Select CONFIRM to host a Relay room <br> or select another mode", "Select CONFIRM to host a Unity Relay room <br> or select another mode", "iphost", "Confirm",
        //         (string connectInput, int connectPort, string playerName, OnlineMode onlineMode) =>
        //     {
        //         m_GameNetPortal.PlayerName = playerName;
        //         switch (onlineMode)
        //         {
        //             case OnlineMode.Relay:
        //                 m_GameNetPortal.StartPhotonRelayHost(connectInput);
        //                 break;
        //
        //             case OnlineMode.IpHost:
        //                 m_GameNetPortal.StartHost(PostProcessIpInput(connectInput), connectPort);
        //                 break;
        //
        //             case OnlineMode.UnityRelay:
        //                 Debug.Log("Unity Relay Host clicked");
        //                 m_GameNetPortal.StartUnityRelayHost();
        //                 break;
        //         }
        //         m_ResponsePopup.SetupNotifierDisplay("Starting host", "Attempting to Start host...", true, false);
        //     }, k_DefaultIP, k_ConnectPort);
        // }

        /// <summary>
        /// Callback when the server sends us back a connection finished event.
        /// </summary>
        /// <param name="status"></param>
        private void OnConnectFinished(ConnectStatus status)
        {
            ConnectStatusToMessage(status, true);
        }

        /// <summary>
        /// Takes a ConnectStatus and shows an appropriate message to the user. This can be called on: (1) successful connect,
        /// (2) failed connect, (3) disconnect.
        /// </summary>
        /// <param name="connecting">pass true if this is being called in response to a connect finishing.</param>
        private void ConnectStatusToMessage(ConnectStatus status, bool connecting)
        {
            // switch(status)
            // {
            //     case ConnectStatus.Undefined:
            //     case ConnectStatus.UserRequestedDisconnect:
            //         break;
            //     case ConnectStatus.ServerFull:
            //         m_ResponsePopup.SetupNotifierDisplay("Connection Failed", "The Host is full and cannot accept any additional connections", false, true);
            //         break;
            //     case ConnectStatus.Success:
            //         if(connecting) { m_ResponsePopup.SetupNotifierDisplay("Success!", "Joining Now", false, true); }
            //         break;
            //     case ConnectStatus.LoggedInAgain:
            //         m_ResponsePopup.SetupNotifierDisplay("Connection Failed", "You have logged in elsewhere using the same account", false, true);
            //         break;
            //     case ConnectStatus.GenericDisconnect:
            //         var title = connecting ? "Connection Failed" : "Disconnected From Host";
            //         var text = connecting ? "Something went wrong" : "The connection to the host was lost";
            //         m_ResponsePopup.SetupNotifierDisplay(title, text, false, true);
            //         break;
            //     default:
            //         Debug.LogWarning($"New ConnectStatus {status} has been added, but no connect message defined for it.");
            //         break;
            // }
        }

        /// <summary>
        /// This should allow us to push a message pop up for connection responses from within other classes
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="displayImage"></param>
        /// <param name="displayConfirmation"></param>
        public void PushConnectionResponsePopup(string title, string message, bool displayImage, bool displayConfirmation)
        {
            // m_ResponsePopup.SetupNotifierDisplay(title, message, displayImage, displayConfirmation);
        }

        /// <summary>
        /// Invoked when the client sent a connection request to the server and didn't hear back at all.
        /// This should create a UI letting the player know that something went wrong and to try again
        /// </summary>
        private void OnNetworkTimeout()
        {
            // m_ResponsePopup.SetupNotifierDisplay("Connection Failed", "Unable to Reach Host/Server", false, true, "Please try again");
        }

        private void OnRelayJoinFailed(string message)
        {
            PushConnectionResponsePopup("Unity Relay: Join Failed", $"{message}", true, true);
        }

        public override void OnDestroy()
        {
            m_Scope?.Dispose();

            if (m_ClientNetPortal != null)
            {
                m_ClientNetPortal.NetworkTimedOut -= OnNetworkTimeout;
                m_ClientNetPortal.ConnectFinished -= OnConnectFinished;
                m_ClientNetPortal.OnUnityRelayJoinFailed -= OnRelayJoinFailed;
            }

            base.OnDestroy();
        }
    }
}
