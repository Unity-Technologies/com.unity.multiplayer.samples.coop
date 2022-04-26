using System;
using Unity.Multiplayer.Samples.BossRoom.Shared;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    public class UIQuitPanel : MonoBehaviour
    {
        enum QuitMode
        {
            ReturnToMenu,
            QuitApplication
        }

        [SerializeField]
        QuitMode m_QuitMode = QuitMode.ReturnToMenu;

        ApplicationController m_ApplicationController;


        [Inject]
        void InjectDependencies(ApplicationController applicationController)
        {
            m_ApplicationController = applicationController;
        }

        public void Quit()
        {
            switch (m_QuitMode)
            {
                case QuitMode.ReturnToMenu:
                    m_ApplicationController.LeaveSession(true);
                    break;
                case QuitMode.QuitApplication:
                    m_ApplicationController.QuitGame();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            gameObject.SetActive(false);
        }
    }
}
