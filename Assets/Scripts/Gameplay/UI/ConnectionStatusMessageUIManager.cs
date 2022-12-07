using System;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// Subscribes to connection status messages to display them through the popup panel.
    /// </summary>
    public class ConnectionStatusMessageUIManager : MonoBehaviour
    {
        DisposableGroup m_Subscriptions;

        PopupPanel m_CurrentReconnectPopup;

        [Inject]
        void InjectDependencies(ISubscriber<ConnectStatus> connectStatusSub, ISubscriber<ReconnectMessage> reconnectMessageSub)
        {
            m_Subscriptions = new DisposableGroup();
            m_Subscriptions.Add(connectStatusSub.Subscribe(OnConnectStatus));
            m_Subscriptions.Add(reconnectMessageSub.Subscribe(OnReconnectMessage));
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
                case ConnectStatus.Success:
                    break;
                case ConnectStatus.Disconnected:
                    if (!string.IsNullOrEmpty(NetworkManager.Singleton.DisconnectReason))
                    {
                        PopupManager.ShowPopupPanel("Disconnected From Host", NetworkManager.Singleton.DisconnectReason);
                    }
                    else
                    {
                        PopupManager.ShowPopupPanel("Disconnected From Host", "The connection to the host was lost.");
                    }
                    break;
                case ConnectStatus.Reconnecting:
                    break;
                case ConnectStatus.StartHostFailed:
                    PopupManager.ShowPopupPanel("Connection Failed", "Starting host failed.");
                    break;
                case ConnectStatus.StartClientFailed:
                    PopupManager.ShowPopupPanel("Connection Failed", "Starting client failed.");
                    break;
                case ConnectStatus.ConnectionDenied:
                    PopupManager.ShowPopupPanel("Connection Denied", NetworkManager.Singleton.DisconnectReason);
                    break;
                default:
                    Debug.LogWarning($"New ConnectStatus {status} has been added, but no connect message defined for it.");
                    break;
            }
        }

        void OnReconnectMessage(ReconnectMessage message)
        {
            if (message.CurrentAttempt == message.MaxAttempt)
            {
                CloseReconnectPopup();
            }
            else if (m_CurrentReconnectPopup != null)
            {
                m_CurrentReconnectPopup.SetupPopupPanel("Connection lost", $"Attempting to reconnect...\nAttempt {message.CurrentAttempt + 1}/{message.MaxAttempt}", closeableByUser: false);
            }
            else
            {
                m_CurrentReconnectPopup = PopupManager.ShowPopupPanel("Connection lost", $"Attempting to reconnect...\nAttempt {message.CurrentAttempt + 1}/{message.MaxAttempt}", closeableByUser: false);
            }
        }

        void CloseReconnectPopup()
        {
            if (m_CurrentReconnectPopup != null)
            {
                m_CurrentReconnectPopup.Hide();
                m_CurrentReconnectPopup = null;
            }
        }
    }
}
