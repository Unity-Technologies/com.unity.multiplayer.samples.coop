using System;
using Unity.Multiplayer.Samples.BossRoom.ApplicationLifecycle.Messages;
using Unity.Multiplayer.Samples.BossRoom.Shared;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using UnityEngine;
using VContainer;

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

        [Inject]
        IPublisher<QuitGameSessionMessage> m_QuitGameSessionPub;

        [Inject]
        IPublisher<QuitApplicationMessage> m_QuitApplicationPub;

        public void Quit()
        {
            switch (m_QuitMode)
            {
                case QuitMode.ReturnToMenu:
                    m_QuitGameSessionPub.Publish(new QuitGameSessionMessage() { UserRequested = true });
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
