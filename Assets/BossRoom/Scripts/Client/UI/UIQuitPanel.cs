using Unity.Multiplayer.Samples.BossRoom.Shared;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    public class UIQuitPanel : MonoBehaviour
    {
        [SerializeField]
        Text m_QuitButtonText;

        ApplicationController m_ApplicationController;

        [Inject]
        private void InjectDependencies(ApplicationController applicationController)
        {
            m_ApplicationController = applicationController;
        }

        void OnEnable()
        {
            m_QuitButtonText.text = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening ?
                "Leave session?" :
                "Exit Game?";
        }

        public void Quit()
        {
            m_ApplicationController.QuitGame();
            gameObject.SetActive(false);
        }
    }
}
