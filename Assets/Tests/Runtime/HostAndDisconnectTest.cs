using System;
using System.Collections;
using Unity.BossRoom.Gameplay.GameState;
using Unity.BossRoom.Gameplay.UI;
using NUnit.Framework;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;

namespace Unity.BossRoom.Tests.Runtime
{
    public class HostAndDisconnectTest
    {
        const string k_BootstrapSceneName = "Startup";

        const string k_MainMenuSceneName = "MainMenu";

        const string k_CharSelectSceneName = "CharSelect";

        const string k_BossRoomSceneName = "BossRoom";

        static int[] s_PlayerIndices = new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };

        NetworkManager m_NetworkManager;

        IEnumerator WaitUntilMainMenuSceneIsLoaded()
        {
            // load Bootstrap scene
            SceneManager.LoadSceneAsync(k_BootstrapSceneName);

            // validate the loading of project's Bootstrap scene
            yield return new TestUtilities.WaitForSceneLoad(k_BootstrapSceneName);

            // Bootstrap scene is loaded, containing NetworkManager instance; cache it
            m_NetworkManager = NetworkManager.Singleton;

            Assert.That(m_NetworkManager != null);

            // MainMenu is loaded as soon as Startup scene is launched, validate it is loaded
            yield return new TestUtilities.WaitForSceneLoad(k_MainMenuSceneName);

            yield return null;
        }

        IEnumerator WaitUntilCharacterIsSelectedAndReady(int playerIndex)
        {
            yield return new TestUtilities.WaitForSceneLoad(k_CharSelectSceneName);

            yield return null;

            // select a Character
            var seatObjectName = $"PlayerSeat ({playerIndex})";
            var playerSeat = GameObject.Find(seatObjectName);
            Assert.That(playerSeat != null, $"{seatObjectName} not found!");

            var uiCharSelectPlayerSeat = playerSeat.GetComponent<UICharSelectPlayerSeat>();
            Assert.That(uiCharSelectPlayerSeat != null,
                $"{nameof(UICharSelectPlayerSeat)} component not found on {playerSeat}!");
            uiCharSelectPlayerSeat.OnClicked();

            // selecting a class will enable the "Ready" button, next frame it is selectable
            yield return null;

            // hit ready
            ClientCharSelectState.Instance.OnPlayerClickedReady();
        }

        /// <summary>
        /// For now, just tests that the host has entered the BossRoom scene. Can become more complex in the future
        /// (eg. testing networked abilities)
        /// </summary>
        /// <returns></returns>
        IEnumerator WaitUntilBossRoomSceneIsLoaded()
        {
            yield return TestUtilities.AssertIsNetworkSceneLoaded(k_BossRoomSceneName, m_NetworkManager.SceneManager);
        }

        IEnumerator WaitUntilDisconnectedAndMainMenuSceneIsLoaded()
        {
            // once loaded into BossRoom scene, disconnect
            var uiSettingsCanvas = GameObject.FindObjectOfType<UISettingsCanvas>();
            Assert.That(uiSettingsCanvas != null, $"{nameof(UISettingsCanvas)} component not found!");
            uiSettingsCanvas.OnClickQuitButton();

            yield return new WaitForFixedUpdate();

            var uiQuitPanel = GameObject.FindObjectOfType<UIQuitPanel>(true);
            Assert.That(uiQuitPanel != null, $"{nameof(UIQuitPanel)} component not found!");
            uiQuitPanel.Quit();

            // Netcode TODO: OnNetworkDespawn() errors pop up here
            // Line below should not be necessary, logged here: https://jira.unity3d.com/browse/MTT-3376
            yield return new WaitForSeconds(1f);

            // wait until shutdown is complete
            yield return new WaitUntil(() => !m_NetworkManager.ShutdownInProgress);

            Assert.That(!NetworkManager.Singleton.IsListening, "NetworkManager not fully shut down!");

            // MainMenu is loaded as soon as a shutdown is encountered; validate it is loaded
            yield return new TestUtilities.WaitForSceneLoad(k_MainMenuSceneName);
        }

        /// <summary>
        /// Smoke test to validating hosting inside Boss Room. The test will load the project's bootstrap scene,
        /// Startup, and commence the game IP flow as a host, pick and confirm a parametrized character, and jump into
        /// the BossRoom scene, where the test will disconnect the host.
        /// </summary>
        [UnityTest]
        public IEnumerator IP_HostAndDisconnect_Valid([ValueSource(nameof(s_PlayerIndices))] int playerIndex)
        {
            yield return WaitUntilMainMenuSceneIsLoaded();

            var clientMainMenuState = GameObject.FindObjectOfType<ClientMainMenuState>();

            Assert.That(clientMainMenuState != null, $"{nameof(clientMainMenuState)} component not found!");

            var container = clientMainMenuState.Container;
            var ipUIMediator = container.Resolve<IPUIMediator>();

            Assert.That(ipUIMediator != null, $"{nameof(IPUIMediator)} component not found!");

            var ipHostingUI = ipUIMediator.IPHostingUI;
            Assert.That(ipHostingUI != null, $"{nameof(IPHostingUI)} component not found!");

            // select "DIRECT IP" button
            clientMainMenuState.OnDirectIPClicked();

            yield return null;

            // select the "HOST" button
            ipHostingUI.OnCreateClick();

            // confirming hosting will initialize the hosting process; next frame the results will be ready
            yield return null;

            // verify hosting is successful
            Assert.That(m_NetworkManager.IsListening && m_NetworkManager.IsHost);

            // CharSelect is loaded as soon as hosting is successful, validate it is loaded
            yield return WaitUntilCharacterIsSelectedAndReady(playerIndex);

            // selecting ready as host with no other party members will load BossRoom scene; validate it is loaded
            yield return WaitUntilBossRoomSceneIsLoaded();

            // Netcode TODO: the line below prevents a NullReferenceException on NetworkSceneManager.OnSceneLoaded
            // Line below should not be necessary, logged here: https://jira.unity3d.com/browse/MTT-3376
            yield return new WaitForSeconds(2f);

            yield return WaitUntilDisconnectedAndMainMenuSceneIsLoaded();
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
