using System.Collections;
using NUnit.Framework;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
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

        /// <summary>
        /// Smoke test to validating hosting inside Boss Room. The test will load the project's bootstrap scene,
        /// Startup, and commence the game flow as a host, pick and confirm a parametrized character, and jump into the
        /// BossRoom scene, where the test will disconnect the host.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator BossRoom_HostAndDisconnect_Valid([ValueSource(nameof(s_PlayerIndices))] int playerIndex)
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

            // now inside MainMenu scene; create a host

            var mainMenuUI = GameObject.FindObjectOfType<MainMenuUI>();

            Assert.That(mainMenuUI != null, "MainMenuUI component not found!");

            mainMenuUI.OnHostClicked();

            // a confirmation popup will appear; wait a frame for it to pop up
            yield return new WaitForEndOfFrame();

            TestUtilities.ClickButtonByName("Confirmation Button");

            // confirming hosting will initialize the hosting process; next frame the results will be ready
            yield return null;

            // verify hosting is successful
            Assert.That(m_NetworkManager.IsListening);

            // CharSelect is loaded as soon as hosting is successful, validate it is loaded
            yield return TestUtilities.AssertIsSceneLoaded(k_CharSelectSceneName);

            yield return new WaitForEndOfFrame();

            // select first Character
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

            // selecting ready as host with no other party members will load BossRoom scene; validate it is loaded
            yield return TestUtilities.AssertIsNetworkSceneLoaded(k_BossRoomSceneName, m_NetworkManager.SceneManager);

            // once loaded into BossRoom scene, disconnect
            var uiQuitPanel = GameObject.FindObjectOfType<UIQuitPanel>(true);
            Assert.That(uiQuitPanel != null, $"{nameof(UIQuitPanel)} component not found!");
            uiQuitPanel.Quit();

            // wait until shutdown is complete
            yield return new WaitUntil(() => !m_NetworkManager.ShutdownInProgress);

            Assert.That(!NetworkManager.Singleton.IsListening, "NetworkManager not fully shut down!");

            // MainMenu is loaded as soon as a shutdown is encountered; validate it is loaded
            yield return TestUtilities.AssertIsSceneLoaded(k_MainMenuSceneName);
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
