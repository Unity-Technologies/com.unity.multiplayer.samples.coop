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

        private bool m_QuitMode = true;

        [Inject]
        private void InjectDependencies(ApplicationController applicationController)
        {
            m_ApplicationController = applicationController;
        }

        void OnEnable()
        {
            m_QuitMode = SceneManager.GetActiveScene().name == "MainMenu";
            m_QuitButtonText.text = m_QuitMode ? "Exit Game?" : "Return to menu?";
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
