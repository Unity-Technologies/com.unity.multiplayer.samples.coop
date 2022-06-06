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
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Action = Unity.Multiplayer.Samples.BossRoom.Server.Action;

namespace Unity.Multiplayer.Samples.BossRoom.Shared
{
    /// <summary>
    /// An entry point to the application, where we bind all the common dependencies to the root DI scope.
    /// </summary>
    public class ApplicationController : MonoBehaviour
    {
        [SerializeField] UpdateRunner m_UpdateRunner;
        [SerializeField] GameNetPortal m_GameNetPortal;
        [SerializeField] ClientGameNetPortal m_ClientNetPortal;
        [SerializeField] ServerGameNetPortal m_ServerGameNetPortal;

        LocalLobby m_LocalLobby;
        LobbyServiceFacade m_LobbyServiceFacade;
        IDisposable m_Subscriptions;

        [SerializeField] GameObject[] m_GameObjectsThatWillBeInjectedAutomatically;

        private void Awake()
        {
            Application.wantsToQuit += OnWantToQuit;

            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(m_UpdateRunner.gameObject);

            // TMP DO NOT COMMIT
            UserNetworkVariableSerialization<WinState>.WriteValue += (FastBufferWriter writer, in WinState value) => writer.WriteValueSafe(value);
            UserNetworkVariableSerialization<WinState>.ReadValue += (FastBufferReader reader, out WinState value) => reader.ReadValueSafe(out value);
            UserNetworkVariableSerialization<MovementStatus>.ReadValue += (FastBufferReader reader, out MovementStatus value) => reader.ReadValueSafe(out value);
            UserNetworkVariableSerialization<MovementStatus>.WriteValue += (FastBufferWriter writer, in MovementStatus value) => writer.WriteValueSafe(value);
            UserNetworkVariableSerialization<LifeState>.ReadValue += (FastBufferReader reader, out LifeState value) => reader.ReadValueSafe(out value);
            UserNetworkVariableSerialization<LifeState>.WriteValue += (FastBufferWriter writer, in LifeState value) => writer.WriteValueSafe(value);
            UserNetworkVariableSerialization<Action.BuffableValue>.ReadValue += (FastBufferReader reader, out Action.BuffableValue value) => reader.ReadValueSafe(out value);
            UserNetworkVariableSerialization<Action.BuffableValue>.WriteValue += (FastBufferWriter writer, in Action.BuffableValue value) => writer.WriteValueSafe(value);
            UserNetworkVariableSerialization<Action.GameplayActivity>.ReadValue += (FastBufferReader reader, out Action.GameplayActivity value) => reader.ReadValueSafe(out value);
            UserNetworkVariableSerialization<Action.GameplayActivity>.WriteValue += (FastBufferWriter writer, in Action.GameplayActivity value) => writer.WriteValueSafe(value);
            UserNetworkVariableSerialization<ActionType>.ReadValue += (FastBufferReader reader, out ActionType value) => reader.ReadValueSafe(out value);
            UserNetworkVariableSerialization<ActionType>.WriteValue += (FastBufferWriter writer, in ActionType value) => writer.WriteValueSafe(value);
            UserNetworkVariableSerialization<ActionLogic>.ReadValue += (FastBufferReader reader, out ActionLogic value) => reader.ReadValueSafe(out value);
            UserNetworkVariableSerialization<ActionLogic>.WriteValue += (FastBufferWriter writer, in ActionLogic value) => writer.WriteValueSafe(value);
            UserNetworkVariableSerialization<ActionDescription.BlockingModeType>.ReadValue += (FastBufferReader reader, out ActionDescription.BlockingModeType value) => reader.ReadValueSafe(out value);
            UserNetworkVariableSerialization<ActionDescription.BlockingModeType>.WriteValue += (FastBufferWriter writer, in ActionDescription.BlockingModeType value) => writer.WriteValueSafe(value);
            UserNetworkVariableSerialization<ConnectStatus>.ReadValue += (FastBufferReader reader, out ConnectStatus value) => reader.ReadValueSafe(out value);
            UserNetworkVariableSerialization<ConnectStatus>.WriteValue += (FastBufferWriter writer, in ConnectStatus value) => writer.WriteValueSafe(value);
            UserNetworkVariableSerialization<CharSelectData.SeatState>.ReadValue += (FastBufferReader reader, out CharSelectData.SeatState value) => reader.ReadValueSafe(out value);
            UserNetworkVariableSerialization<CharSelectData.SeatState>.WriteValue += (FastBufferWriter writer, in CharSelectData.SeatState value) => writer.WriteValueSafe(value);
            UserNetworkVariableSerialization<GameState>.ReadValue += (FastBufferReader reader, out GameState value) => reader.ReadValueSafe(out value);
            UserNetworkVariableSerialization<GameState>.WriteValue += (FastBufferWriter writer, in GameState value) => writer.WriteValueSafe(value);
            UserNetworkVariableSerialization<CharacterSwap.SpecialMaterialMode>.ReadValue += (FastBufferReader reader, out CharacterSwap.SpecialMaterialMode value) => reader.ReadValueSafe(out value);
            UserNetworkVariableSerialization<CharacterSwap.SpecialMaterialMode>.WriteValue += (FastBufferWriter writer, in CharacterSwap.SpecialMaterialMode value) => writer.WriteValueSafe(value);
            UserNetworkVariableSerialization<CharacterTypeEnum>.ReadValue += (FastBufferReader reader, out CharacterTypeEnum value) => reader.ReadValueSafe(out value);
            UserNetworkVariableSerialization<CharacterTypeEnum>.WriteValue += (FastBufferWriter writer, in CharacterTypeEnum value) => writer.WriteValueSafe(value);
            UserNetworkVariableSerialization<IDamageable.SpecialDamageFlags>.ReadValue += (FastBufferReader reader, out IDamageable.SpecialDamageFlags value) => reader.ReadValueSafe(out value);
            UserNetworkVariableSerialization<IDamageable.SpecialDamageFlags>.WriteValue += (FastBufferWriter writer, in IDamageable.SpecialDamageFlags value) => writer.WriteValueSafe(value);
            UserNetworkVariableSerialization<ClientInputSender.SkillTriggerStyle>.ReadValue += (FastBufferReader reader, out ClientInputSender.SkillTriggerStyle value) => reader.ReadValueSafe(out value);
            UserNetworkVariableSerialization<ClientInputSender.SkillTriggerStyle>.WriteValue += (FastBufferWriter writer, in ClientInputSender.SkillTriggerStyle value) => writer.WriteValueSafe(value);

            var scope = DIScope.RootScope;

            scope.BindInstanceAsSingle(this);
            scope.BindInstanceAsSingle(m_UpdateRunner);
            scope.BindInstanceAsSingle(m_GameNetPortal);
            scope.BindInstanceAsSingle(m_ClientNetPortal);
            scope.BindInstanceAsSingle(m_ServerGameNetPortal);

            //the following singletons represent the local representations of the lobby that we're in and the user that we are
            //they can persist longer than the lifetime of the UI in MainMenu where we set up the lobby that we create or join
            scope.BindAsSingle<LocalLobbyUser>();
            scope.BindAsSingle<LocalLobby>();

            scope.BindAsSingle<ProfileManager>();

            //these message channels are essential and persist for the lifetime of the lobby and relay services
            scope.BindMessageChannelInstance<QuitGameSessionMessage>();
            scope.BindMessageChannelInstance<QuitApplicationMessage>();
            scope.BindMessageChannelInstance<UnityServiceErrorMessage>();
            scope.BindMessageChannelInstance<ConnectStatus>();
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

            var quitGameSessionSub = scope.Resolve<ISubscriber<QuitGameSessionMessage>>();
            var quitApplicationSub = scope.Resolve<ISubscriber<QuitApplicationMessage>>();

            var subHandles = new DisposableGroup();
            subHandles.Add(quitGameSessionSub.Subscribe(LeaveSession));
            subHandles.Add(quitApplicationSub.Subscribe(QuitGame));
            m_Subscriptions = subHandles;

            Application.targetFrameRate = 120;
        }

        private void Start()
        {
            SceneManager.LoadScene("MainMenu");
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

        // TODO remove messaging for this once we have vcontainer.
        private void LeaveSession(QuitGameSessionMessage msg)
        {
            m_LobbyServiceFacade.EndTracking();

            if (msg.UserRequested)
            {
                // first disconnect then return to menu
                var gameNetPortal = GameNetPortal.Instance;
                if (gameNetPortal != null)
                {
                    gameNetPortal.RequestDisconnect();
                }
            }
            SceneLoaderWrapper.Instance.LoadScene("MainMenu", useNetworkSceneManager: false);
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
