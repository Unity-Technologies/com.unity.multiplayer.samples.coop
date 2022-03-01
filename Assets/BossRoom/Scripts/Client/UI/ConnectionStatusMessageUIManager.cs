using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using UnityEngine;

public class ConnectionStatusMessageUIManager : MonoBehaviour
{

    DisposableGroup m_Subscriptions = new DisposableGroup();

    [Inject]
    void InjectDependencies(ISubscriber<ConnectStatus> connectStatusSub,
        ISubscriber<UnityServiceErrorMessage> unityServiceErrorMessageSub)
    {
        m_Subscriptions.Add(connectStatusSub.Subscribe(OnConnectStatus));
        m_Subscriptions.Add(unityServiceErrorMessageSub.Subscribe(OnUnityServiceErrorMessage));
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        m_Subscriptions?.Dispose();
    }

    void OnUnityServiceErrorMessage(UnityServiceErrorMessage unityServiceErrorMessage)
    {
        PopupPanel.ShowPopupPanel(unityServiceErrorMessage.Title, unityServiceErrorMessage.Message);
    }

    void OnConnectStatus(ConnectStatus status)
    {
        switch (status)
        {
            case ConnectStatus.Undefined:
            case ConnectStatus.UserRequestedDisconnect:
                break;
            case ConnectStatus.ServerFull:
                PopupPanel.ShowPopupPanel("Connection Failed", "The Host is full and cannot accept any additional connections.");
                break;
            case ConnectStatus.Success:
                break;
            case ConnectStatus.LoggedInAgain:
                PopupPanel.ShowPopupPanel("Connection Failed", "You have logged in elsewhere using the same account.");
                break;
            case ConnectStatus.GenericDisconnect:
                PopupPanel.ShowPopupPanel("Disconnected From Host", "The connection to the host was lost");
                break;
            default:
                Debug.LogWarning($"New ConnectStatus {status} has been added, but no connect message defined for it.");
                break;
        }
    }
}
