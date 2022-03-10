using System;
using Unity.Multiplayer.Samples.BossRoom.Shared;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    public class UIQuitPanel : MonoBehaviour
    {
        [SerializeField]
        Text m_QuitButtonText;

        ApplicationController m_ApplicationController;

        bool m_QuitMode = true;

        [Inject]
        void InjectDependencies(ApplicationController applicationController)
        {
            m_ApplicationController = applicationController;
        }

        void Awake()
        {
            SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
        }

        void SceneManagerOnsceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            var currentGameState = FindObjectOfType<GameStateBehaviour>();
            if (currentGameState != null)
            {
                m_QuitMode = (currentGameState.ActiveState == GameState.MainMenu);
                m_QuitButtonText.text = m_QuitMode ? "Exit Game?" : "Return to menu?";
            }
            else
            {
                Debug.LogError($"Scene {scene.name} does not contain a GameStateBehavior");
            }
        }

        public void Quit()
        {
            if (m_QuitMode)
            {
                m_ApplicationController.QuitGame();
            }
            else
            {
                m_ApplicationController.LeaveSession();
            }

            gameObject.SetActive(false);
        }
    }
}
