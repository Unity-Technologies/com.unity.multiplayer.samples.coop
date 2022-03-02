using System;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    public class ConnectionStatusMessageUIManager : MonoBehaviour
    {
        IDisposable m_Subscriptions;
        PopupPanel m_PopupPanel;

        [Inject]
        void InjectDependencies(ISubscriber<ConnectStatus> connectStatusSub, PopupPanel popupPanel)
        {
            m_Subscriptions = connectStatusSub.Subscribe(OnConnectStatus);
            m_PopupPanel = popupPanel;
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            m_Subscriptions?.Dispose();
        }

        void OnConnectStatus(ConnectStatus status)
        {
            switch (status)
            {
                case ConnectStatus.Undefined:
                case ConnectStatus.UserRequestedDisconnect:
                    break;
                case ConnectStatus.ServerFull:
                    m_PopupPanel.ShowPopupPanel("Connection Failed", "The Host is full and cannot accept any additional connections.");
                    break;
                case ConnectStatus.Success:
                    break;
                case ConnectStatus.LoggedInAgain:
                    m_PopupPanel.ShowPopupPanel("Connection Failed", "You have logged in elsewhere using the same account.");
                    break;
                case ConnectStatus.GenericDisconnect:
                    m_PopupPanel.ShowPopupPanel("Disconnected From Host", "The connection to the host was lost");
                    break;
                default:
                    Debug.LogWarning($"New ConnectStatus {status} has been added, but no connect message defined for it.");
                    break;
            }
        }
    }
}
