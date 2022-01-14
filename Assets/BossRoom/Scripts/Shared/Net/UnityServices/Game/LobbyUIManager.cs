using System;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using UnityEditor.Experimental.RestService;
using UnityEngine;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Game
{
    //todo: inject into PopupPanel somehow
    public class LobbyUIManager : MonoBehaviour
    {
        private DIScope _container;

        //private LocalGameState m_localGameState = new LocalGameState();
        private LobbyUser m_localUser;
        private LocalLobby m_localLobby;
        private LobbyServiceData m_lobbyServiceData = new LobbyServiceData();
        private LobbyContentHeartbeat m_lobbyContentHeartbeat;


        private void Awake()
        {
            _container = new DIScope();
            
            _container.BindMessageChannel<ClientUserSeekingDisapprovalMessage>();
            _container.BindMessageChannel<DisplayErrorPopupMessage>();
            _container.BindMessageChannel<ChangeGameStateMessage>();
            _container.BindMessageChannel<EndGameMessage>();

            _container.BindAsSingle<LobbyAsyncRequests>();

            _container.BindAsSingle<LocalGameState>();
            _container.BindAsSingle<LobbyUser>();
            _container.BindAsSingle<LobbyServiceData>();
            _container.BindAsSingle<LobbyContentHeartbeat>();

            _container.FinalizeScopeConstruction();
        }

        private void OnDestroy()
        {
            _container?.Dispose();
        }

        private void Awake()
        {
            // Do some arbitrary operations to instantiate singletons.
#pragma warning disable IDE0059 // Unnecessary assignment of a value
            var unused = PlayerDataFileLocator.Locator.Get;
#pragma warning restore IDE0059

            PlayerDataFileLocator.Locator.Get.Provide(new Auth.Identity(OnAuthSignIn));
            Application.wantsToQuit += OnWantToQuit;
        }

        private void Start()
        {
            m_localLobby = new LocalLobby { State = LobbyState.Lobby };
            m_localUser = new LobbyUser();
            m_localUser.DisplayName = "New Player";
            PlayerDataFileLocator.Locator.Get.Messenger.Subscribe(this);
            BeginObservers();
        }

        private void OnAuthSignIn()
        {
            Debug.Log("Signed in.");
            m_localUser.ID = PlayerDataFileLocator.Locator.Get.Identity.GetSubIdentity(Auth.IIdentityType.Auth).GetContent("id");
            m_localUser.DisplayName = NameGenerator.GetName(m_localUser.ID);
            m_localLobby.AddPlayer(m_localUser); // The local LobbyUser object will be hooked into UI before the LocalLobby is populated during lobby join, so the LocalLobby must know about it already when that happens.
        }

        private void BeginObservers()
        {
            foreach (var gameStateObs in m_GameStateObservers)
                gameStateObs.BeginObserving(m_localGameState);
            foreach (var serviceObs in m_LobbyServiceObservers)
                serviceObs.BeginObserving(m_lobbyServiceData);
            foreach (var lobbyObs in m_LocalLobbyObservers)
                lobbyObs.BeginObserving(m_localLobby);
            foreach (var userObs in m_LocalUserObservers)
                userObs.BeginObserving(m_localUser);
        }

        //todo:
        // - *Create a centralized way to subscribe to Update, SlowUpdate, WantsToQuit and other events - use MessageChannel for that? Or reuse SlowUpdate
        // - Before we go into any kind of state that would require us to have wired dependencies - we should already had created a DIScope with dependencies
        //    - for the purposes of wiring the Lobby code we can just create a DI Scope within the PopupPanel (or some other class that would serve as entrypoint into this whole login logic)
        // - we want to start by injecting dependencies into our UI objects
        // - we want to suscribe to various events


    }
}
