using System;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Infrastructure;
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
            if (m_Subscriptions != null)
            {
                m_Subscriptions.Dispose();
            }
        }

        void OnConnectStatus(ConnectStatus status)
        {
            switch (status)
            {
                case ConnectStatus.Undefined:
                case ConnectStatus.UserRequestedDisconnect:
                    break;
                case ConnectStatus.ServerFull:
                    PopupManager.ShowPopupPanel("Connection Failed", "The Host is full and cannot accept any additional connections.");
                    break;
                case ConnectStatus.Success:
                    break;
                case ConnectStatus.LoggedInAgain:
                    PopupManager.ShowPopupPanel("Connection Failed", "You have logged in elsewhere using the same account. If you still want to connect, select a different profile by using the 'Change Profile' button.");
                    break;
                case ConnectStatus.IncompatibleBuildType:
                    PopupManager.ShowPopupPanel("Connection Failed", "Server and client builds are not compatible. You cannot connect a release build to a development build or an in-editor session.");
                    break;
                case ConnectStatus.GenericDisconnect:
                    PopupManager.ShowPopupPanel("Disconnected From Host", "The connection to the host was lost.");
                    break;
                case ConnectStatus.HostEndedSession:
                    PopupManager.ShowPopupPanel("Disconnected From Host", "The host has ended the game session.");
                    break;
                case ConnectStatus.Reconnecting:
                    break;
                case ConnectStatus.StartHostFailed:
                    PopupManager.ShowPopupPanel("Connection Failed", "Starting host failed.");
                    break;
                case ConnectStatus.StartClientFailed:
                    PopupManager.ShowPopupPanel("Connection Failed", "Starting client failed.");
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
