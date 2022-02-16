using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unity.Multiplayer.Samples.Utilities
{
    public abstract class TestUtils
    {
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
        /// <returns></returns>
        public static IEnumerator AssertIsSceneLoaded(string sceneName)
        {
            var waitUntilSceneLoaded = new WaitForSceneLoad(sceneName);

            yield return waitUntilSceneLoaded;

            Assert.That(!waitUntilSceneLoaded.timedOut);
        }
    }

    /// <summary>
    /// Custom IEnumerator class to validate the loading of a Scene by name. If a scene load lasts longer than
    /// k_MaxSceneLoadDuration it is considered a timeout.
    /// </summary>
    public class WaitForSceneLoad : CustomYieldInstruction
    {
        const float k_MaxSceneLoadDuration = 10f;

        string m_SceneName;

        float m_LoadSceneStart;

        float m_MaxLoadDuration;

        public bool timedOut { get; private set; }

        public override bool keepWaiting
        {
            get
            {
                var scene = SceneManager.GetSceneByName(m_SceneName);

                var isSceneLoaded = scene.IsValid() && scene.isLoaded;

                if (Time.time - m_LoadSceneStart >= m_MaxLoadDuration)
                {
                    timedOut = true;
                }

                return !isSceneLoaded && !timedOut;
            }
        }

        public WaitForSceneLoad(string sceneName, float maxLoadDuration = k_MaxSceneLoadDuration)
        {
            m_LoadSceneStart = Time.time;
            m_SceneName = sceneName;
            m_MaxLoadDuration = maxLoadDuration;
        }
    }
}
