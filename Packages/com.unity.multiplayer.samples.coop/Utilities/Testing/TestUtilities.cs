using System.Collections;
using UnityEngine;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unity.Multiplayer.Samples.Utilities
{
    public abstract class TestUtilities
    {
        const float k_MaxSceneLoadDuration = 10f;

        /// <summary>
        /// Finds an active Button GameObject by name. If Button component is present on GameObject, it will be clicked.
        /// </summary>
        /// <param name="name"> Name of Button GameObject </param>
        public static void ClickButtonByName(string name)
        {
            var buttonGameObject = GameObject.Find(name);

            Assert.IsNotNull(buttonGameObject,
                $"Button GameObject with name {name} not found in scene!");

            EventSystem.current.SetSelectedGameObject(buttonGameObject);

            var buttonComponent = buttonGameObject.GetComponent<Button>();

            Assert.IsNotNull(buttonComponent, $"Button component not found on {buttonGameObject.name}!");

            buttonComponent.onClick.Invoke();
        }

        /// <summary>
        /// Helper wrapper method for asserting the completion of a scene load to be used inside Playmode tests. A scene
        /// is either loaded successfully, or the loading process has timed out.
        /// </summary>
        /// <param name="sceneName"> Name of scene </param>
        /// <returns> IEnumerator to track scene load process </returns>
        public static IEnumerator AssertIsSceneLoaded(string sceneName)
        {
            var waitUntilSceneLoaded = new WaitForSceneLoad(sceneName);

            yield return waitUntilSceneLoaded;

            Assert.That(!waitUntilSceneLoaded.TimedOut);
        }

        /// <summary>
        /// Helper wrapper method for asserting the completion of a scene load to be used inside Playmode tests. A scene
        /// is either loaded successfully, or the loading process has timed out.
        /// </summary>
        /// <param name="sceneName"> Name of scene </param>
        /// <param name="networkSceneManager"> NetworkSceneManager instance </param>
        /// <returns> IEnumerator to track scene load process </returns>
        public static IEnumerator AssertIsNetworkSceneLoaded(string sceneName, NetworkSceneManager networkSceneManager)
        {
            Assert.That(networkSceneManager != null, "NetworkSceneManager instance is null!");

            var waitForNetworkSceneLoad = new WaitForNetworkSceneLoad(sceneName, networkSceneManager);

            yield return waitForNetworkSceneLoad;

            Assert.That(!waitForNetworkSceneLoad.TimedOut);
        }

        /// <summary>
        /// Custom IEnumerator class to validate the loading of a Scene by name. If a scene load lasts longer than
        /// k_MaxSceneLoadDuration it is considered a timeout.
        /// </summary>
        class WaitForSceneLoad : CustomYieldInstruction
        {
            string m_SceneName;

            float m_LoadSceneStart;

            float m_MaxLoadDuration;

            public bool TimedOut { get; private set; }

            public override bool keepWaiting
            {
                get
                {
                    var scene = SceneManager.GetSceneByName(m_SceneName);

                    var isSceneLoaded = scene.IsValid() && scene.isLoaded;

                    if (Time.time - m_LoadSceneStart >= m_MaxLoadDuration)
                    {
                        TimedOut = true;
                    }

                    return !isSceneLoaded && !TimedOut;
                }
            }

            public WaitForSceneLoad(string sceneName, float maxLoadDuration = k_MaxSceneLoadDuration)
            {
                m_LoadSceneStart = Time.time;
                m_SceneName = sceneName;
                m_MaxLoadDuration = maxLoadDuration;
            }
        }

        /// <summary>
        /// Custom IEnumerator class to validate the loading of a Scene by name. If a scene load lasts longer than
        /// k_MaxSceneLoadDuration it is considered a timeout.
        /// </summary>
        class WaitForNetworkSceneLoad : CustomYieldInstruction
        {
            string m_SceneName;

            float m_LoadSceneStart;

            float m_MaxLoadDuration;

            bool m_IsNetworkSceneLoaded;

            NetworkSceneManager m_NetworkSceneManager;

            public bool TimedOut { get; private set; }

            public override bool keepWaiting
            {
                get
                {
                    if (Time.time - m_LoadSceneStart >= m_MaxLoadDuration)
                    {
                        TimedOut = true;

                        m_NetworkSceneManager.OnSceneEvent -= ConfirmSceneLoad;
                    }

                    return !m_IsNetworkSceneLoaded && !TimedOut;
                }
            }

            public WaitForNetworkSceneLoad(string sceneName, NetworkSceneManager networkSceneManager, float maxLoadDuration = k_MaxSceneLoadDuration)
            {
                m_LoadSceneStart = Time.time;
                m_SceneName = sceneName;
                m_MaxLoadDuration = maxLoadDuration;

                m_NetworkSceneManager = networkSceneManager;

                m_NetworkSceneManager.OnSceneEvent += ConfirmSceneLoad;
            }

            void ConfirmSceneLoad(SceneEvent sceneEvent)
            {
                if (sceneEvent.SceneName == m_SceneName &&
                    sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
                {
                    m_IsNetworkSceneLoaded = true;

                    m_NetworkSceneManager.OnSceneEvent -= ConfirmSceneLoad;
                }
            }
        }
    }
}
