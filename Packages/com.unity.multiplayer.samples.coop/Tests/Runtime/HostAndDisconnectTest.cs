using System;
using System.Collections;
using NUnit.Framework;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Shared;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Lobbies;
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

        const float k_ServiceQueryTimeout = 10f;

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

            yield return null;

            var clientMainMenuState = GameObject.FindObjectOfType<ClientMainMenuState>();
            Assert.That(clientMainMenuState != null, $"{nameof(clientMainMenuState)} component not found!");

            var scope = clientMainMenuState.DIScope;

            var lobbyUIMediator = scope.Resolve<LobbyUIMediator>();
            Assert.That(lobbyUIMediator != null, $"{nameof(LobbyUIMediator)} component not found!");

            var lobbyCreationUI = lobbyUIMediator.LobbyCreationUI;
            Assert.That(lobbyCreationUI != null, $"{nameof(LobbyCreationUI)} component not found!");

            // validate unity services login successful

            // wait until authenticated
            var timer = k_ServiceQueryTimeout;
            while (timer > 0f && !AuthenticationService.Instance.IsAuthorized)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            clientMainMenuState.OnStartClicked();

            yield return new WaitForEndOfFrame();

            lobbyUIMediator.ToggleCreateLobbyUI();

            // a confirmation popup will appear; wait a frame for it to pop up
            yield return new WaitForEndOfFrame();

            lobbyCreationUI.OnCreateClick();

            // get LobbyServiceFacade
            var applicationController = GameObject.FindObjectOfType<ApplicationController>();
            Assert.That(applicationController != null, $"{nameof(ApplicationController)} component not found!");

            var lobbyServiceFacade = applicationController.LobbyServiceFacade;
            Assert.That(lobbyServiceFacade != null, $"{nameof(LobbyServiceFacade)} component not found!");

            // wait until lobby has been created
            timer = k_ServiceQueryTimeout;
            while (timer > 0f && lobbyServiceFacade.CurrentUnityLobby == null)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            Assert.NotNull(lobbyServiceFacade.CurrentUnityLobby);

            // lobby has been created; now relay data will be allocated; wait until lobby relay data updated
            timer = k_ServiceQueryTimeout;
            while (timer > 0f && string.IsNullOrEmpty(lobbyUIMediator.LocalLobby.RelayJoinCode))
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(!string.IsNullOrEmpty(lobbyUIMediator.LocalLobby.RelayJoinCode));

            // lobby creation will initialize the hosting process; next frame the results will be ready
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

            // now inside MainMenu scene

            yield return null;

            var clientMainMenuState = GameObject.FindObjectOfType<ClientMainMenuState>();

            Assert.That(clientMainMenuState != null, $"{nameof(clientMainMenuState)} component not found!");

            var scope = clientMainMenuState.DIScope;

            var ipUIMediator = scope.Resolve<IPUIMediator>();
            Assert.That(ipUIMediator != null, $"{nameof(IPUIMediator)} component not found!");

            var ipHostingUI = ipUIMediator.IPHostingUI;
            Assert.That(ipHostingUI != null, $"{nameof(IPHostingUI)} component not found!");

            // wait until authenticated?
            var timer = 5f;
            while (timer > 0f && !AuthenticationService.Instance.IsAuthorized)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(AuthenticationService.Instance.IsAuthorized);

            clientMainMenuState.OnDirectIPClicked();

            yield return new WaitForEndOfFrame();

            ipUIMediator.ToggleCreateIPUI();

            // a confirmation popup will appear; wait a frame for it to pop up
            yield return new WaitForEndOfFrame();

            ipHostingUI.OnCreateClick();

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
