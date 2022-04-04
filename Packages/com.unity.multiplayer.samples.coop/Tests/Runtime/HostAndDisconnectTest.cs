using System;
using System.Collections;
using NUnit.Framework;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Multiplayer.Samples.BossRoom.Tests.Runtime
{
    public class HostAndDisconnectTest
    {
        const string k_BootstrapSceneName = "Startup";

        const string k_MainMenuSceneName = "MainMenu";

        const string k_CharSelectSceneName = "CharSelect";

        const string k_BossRoomSceneName = "BossRoom";

        static int[] s_PlayerIndices = new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };

        NetworkManager m_NetworkManager;

        ClientMainMenuState m_ClientMainMenuState;

        LobbyUIMediator m_LobbyUIMediator;

        LobbyCreationUI m_LobbyCreationUI;

        IPUIMediator m_IPUIMediator;

        IPHostingUI m_IPHostingUI;

        [Inject]
        void Initialize(
            ClientMainMenuState clientMainMenuState,
            LobbyUIMediator lobbyUIMediator,
            LobbyCreationUI lobbyCreationUI,
            IPUIMediator ipUiMediator,
            IPHostingUI ipHostingUI
        )
        {
            m_ClientMainMenuState = clientMainMenuState;
            m_LobbyUIMediator = lobbyUIMediator;
            m_LobbyCreationUI = lobbyCreationUI;
            m_IPUIMediator = ipUiMediator;
            m_IPHostingUI = ipHostingUI;
        }

        /// <summary>
        /// Smoke test to validating hosting inside Boss Room. The test will load the project's bootstrap scene,
        /// Startup, and commence the game flow as a host, pick and confirm a parametrized character, and jump into the
        /// BossRoom scene, where the test will disconnect the host.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator Lobby_HostAndDisconnect_Valid([ValueSource(nameof(s_PlayerIndices))] int playerIndex)
        {
            yield return GoToMainMenuScene();

            // now inside MainMenu scene

            // validate unity services login successful

            // wait until authenticated?
            var timer = 5f;
            while (timer > 0f && !AuthenticationService.Instance.IsAuthorized)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            // create a host

            Assert.That(m_ClientMainMenuState != null, $"{nameof(m_ClientMainMenuState)} component not found!");

            m_ClientMainMenuState.OnStartClicked();

            yield return new WaitForEndOfFrame();

            var lobbyUIMediator = GameObject.FindObjectOfType<LobbyUIMediator>();

            Assert.That(lobbyUIMediator != null, $"{nameof(LobbyUIMediator)} component not found!");

            lobbyUIMediator.ToggleCreateLobbyUI();

            // a confirmation popup will appear; wait a frame for it to pop up
            yield return new WaitForEndOfFrame();

            var lobbyCreationUI = GameObject.FindObjectOfType<LobbyCreationUI>();

            Assert.That(lobbyCreationUI != null, $"{nameof(LobbyCreationUI)} component not found!");

            lobbyCreationUI.OnCreateClick();

            // get LobbyServiceFacade through DI

            // confirming hosting will initialize the hosting process; next frame the results will be ready
            yield return null;

            // verify hosting is successful
            Assert.That(m_NetworkManager.IsListening);

            // CharSelect is loaded as soon as hosting is successful, validate it is loaded
            yield return GoToCharacterSelection(playerIndex);

            yield return GoToBossRoomScene();

            yield return Disconnect();
        }

        IEnumerator GoToMainMenuScene()
        {
            // load Bootstrap scene
            SceneManager.LoadSceneAsync(k_BootstrapSceneName);

            // validate the loading of project's Bootstrap scene
            yield return TestUtilities.AssertIsSceneLoaded(k_BootstrapSceneName);

            // Bootstrap scene is loaded, containing NetworkManager instance; cache it
            m_NetworkManager = NetworkManager.Singleton;

            Assert.That(m_NetworkManager != null);

            // MainMenu is loaded as soon as Startup scene is launched, validate it is loaded
            yield return TestUtilities.AssertIsSceneLoaded(k_MainMenuSceneName);

            yield return new WaitForEndOfFrame();
        }

        IEnumerator GoToCharacterSelection(int playerIndex)
        {
            yield return TestUtilities.AssertIsSceneLoaded(k_CharSelectSceneName);

            yield return new WaitForEndOfFrame();

            // select a Character
            var seatObjectName = $"PlayerSeat ({playerIndex})";
            var playerSeat = GameObject.Find(seatObjectName);
            Assert.That(playerSeat != null, $"{seatObjectName} not found!");

            var uiCharSelectPlayerSeat = playerSeat.GetComponent<UICharSelectPlayerSeat>();
            Assert.That(uiCharSelectPlayerSeat != null,
                $"{nameof(UICharSelectPlayerSeat)} component not found on {playerSeat}!");
            uiCharSelectPlayerSeat.OnClicked();

            // selecting a class will enable the "Ready" button, next frame it is selectable
            yield return new WaitForEndOfFrame();

            // hit ready
            ClientCharSelectState.Instance.OnPlayerClickedReady();
        }

        /// <summary>
        /// For now, just tests that the host has entered the BossRoom scene. Can become more complex in the future
        /// (eg. testing networked abilities)
        /// </summary>
        /// <returns></returns>
        IEnumerator GoToBossRoomScene()
        {
            // selecting ready as host with no other party members will load BossRoom scene; validate it is loaded
            yield return TestUtilities.AssertIsNetworkSceneLoaded(k_BossRoomSceneName, m_NetworkManager.SceneManager);
        }

        IEnumerator Disconnect()
        {
            // once loaded into BossRoom scene, disconnect
            var uiSettingsCanvas = GameObject.FindObjectOfType<UISettingsCanvas>();
            Assert.That(uiSettingsCanvas != null, $"{nameof(UISettingsCanvas)} component not found!");
            uiSettingsCanvas.OnClickQuitButton();

            yield return new WaitForFixedUpdate();

            var uiQuitPanel = GameObject.FindObjectOfType<UIQuitPanel>(true);
            Assert.That(uiQuitPanel != null, $"{nameof(UIQuitPanel)} component not found!");
            uiQuitPanel.Quit();

            // TODO: validate with SDK why this is still needed
            yield return new WaitForSeconds(1f);

            // wait until shutdown is complete
            yield return new WaitUntil(() => !m_NetworkManager.ShutdownInProgress);

            Assert.That(!NetworkManager.Singleton.IsListening, "NetworkManager not fully shut down!");

            // MainMenu is loaded as soon as a shutdown is encountered; validate it is loaded
            yield return TestUtilities.AssertIsSceneLoaded(k_MainMenuSceneName);
        }

        [UnityTest]
        public IEnumerator IP_HostAndDisconnect_Valid([ValueSource(nameof(s_PlayerIndices))] int playerIndex)
        {
            yield return GoToMainMenuScene();

            yield return new WaitForSeconds(2f);

            var scope = DIScope.RootScope;
            scope.InjectIn(this);

            // now inside MainMenu scene

            // wait until authenticated?
            var timer = 5f;
            while (timer > 0f && !AuthenticationService.Instance.IsAuthorized)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(AuthenticationService.Instance.IsAuthorized);

            Assert.That(m_ClientMainMenuState != null, $"{nameof(ClientMainMenuState)} component not found!");

            m_ClientMainMenuState.OnDirectIPClicked();

            yield return new WaitForEndOfFrame();

            Assert.That(m_IPUIMediator != null, $"{nameof(IPUIMediator)} component not found!");

            m_IPUIMediator.ToggleCreateIPUI();

            // a confirmation popup will appear; wait a frame for it to pop up
            yield return new WaitForEndOfFrame();

            Assert.That(m_IPHostingUI != null, $"{nameof(IPHostingUI)} component not found!");

            m_IPHostingUI.OnCreateClick();

            // confirming hosting will initialize the hosting process; next frame the results will be ready
            yield return null;

            // verify hosting is successful
            Assert.That(m_NetworkManager.IsListening);

            // CharSelect is loaded as soon as hosting is successful, validate it is loaded
            yield return GoToCharacterSelection(playerIndex);

            yield return GoToBossRoomScene();

            yield return Disconnect();
        }

        [UnityTearDown]
        public IEnumerator DestroySceneGameObjects()
        {
            foreach (var sceneGameObject in GameObject.FindObjectsOfType<GameObject>())
            {
                GameObject.DestroyImmediate(sceneGameObject);
            }
            yield break;
        }
    }
}
