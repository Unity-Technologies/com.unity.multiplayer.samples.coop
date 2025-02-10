using System;
using Unity.BossRoom.ApplicationLifecycle.Messages;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Infrastructure;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
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

        [Inject]
        ConnectionManager m_ConnectionManager;

        [Inject]
        IPublisher<QuitApplicationMessage> m_QuitApplicationPub;

        void Awake()
        {
            if (m_ConnectionManager == null)
            {
                Debug.LogError("ConnectionManager is null! Ensure that dependency injection is set up correctly.");
            }
            else
            {
                Debug.Log("ConnectionManager successfully injected into UIQuitPanel.");
            }
        }
        
        public void Quit()
        {
            switch (m_QuitMode)
            {
                case QuitMode.ReturnToMenu:
                    m_ConnectionManager.RequestShutdown();
                    break;
                case QuitMode.QuitApplication:
                    m_QuitApplicationPub.Publish(new QuitApplicationMessage());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            gameObject.SetActive(false);
        }
    }
}
