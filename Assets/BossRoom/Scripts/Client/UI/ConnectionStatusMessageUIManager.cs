using System;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// Subscribes to connection status messages to display them through the popup panel.
    /// </summary>
    public class ConnectionStatusMessageUIManager : MonoBehaviour
    {
        IDisposable m_Subscriptions;

        [Inject]
        void InjectDependencies(ISubscriber<ConnectStatus> connectStatusSub)
        {
            m_Subscriptions = connectStatusSub.Subscribe(OnConnectStatus);
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
                    break;
                case ConnectStatus.UserRequestedDisconnect:
                    PopupPanel.ClosePopupPanel();
                    break;
                case ConnectStatus.ServerFull:
                    PopupPanel.ShowPopupPanel("Connection Failed", "The Host is full and cannot accept any additional connections.");
                    break;
                case ConnectStatus.Success:
                    PopupPanel.ClosePopupPanel();
                    PopupPanel.ShowPopupPanel("Success!", "Joining Now", false);
                    SceneManager.sceneLoaded += ClosePopupOnsceneLoaded;
                    break;
                case ConnectStatus.LoggedInAgain:
                    PopupPanel.ShowPopupPanel("Connection Failed", "You have logged in elsewhere using the same account.");
                    break;
                case ConnectStatus.GenericDisconnect:
                    PopupPanel.ShowPopupPanel("Disconnected From Host", "The connection to the host was lost");
                    break;
                case ConnectStatus.Reconnecting:
                    PopupPanel.ShowPopupPanel("Attempting reconnection", "Lost connection to the Host, attempting to reconnect...", false);
                    break;
                default:
                    Debug.LogWarning($"New ConnectStatus {status} has been added, but no connect message defined for it.");
                    break;
            }
        }

        void ClosePopupOnsceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PopupPanel.ClosePopupPanel();
            SceneManager.sceneLoaded -= ClosePopupOnsceneLoaded;
        }
    }
}
