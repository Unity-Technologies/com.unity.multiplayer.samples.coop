using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using Unity.Services.Lobbies;
using UnityEngine;

public class UIServices : MonoBehaviour
{
    private IDisposable m_Subscriptions;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    [Inject]
    void Initialize(ISubscriber<UnityServiceErrorMessage> serviceError)
    {
        m_Subscriptions = serviceError.Subscribe(ServiceErrorHandler);
    }

    private void ServiceErrorHandler(UnityServiceErrorMessage error)
    {
        var errorMessage = error.Message;
        if (error.AffectedService == UnityServiceErrorMessage.Service.Lobby)
        {
            if ((error.OriginalException as LobbyServiceException).Reason == LobbyExceptionReason.LobbyConflict)
            {
                // LobbyConflict can have multiple causes. Let's add other solutions here if there's other situations that arise for this.
                errorMessage += "\nSee logs for possible causes and solution.";
                Debug.LogError($"Got service error {error.Message} with LobbyConflict. Possible conflict cause: Trying to play with two builds on the " +
                               $"same machine. Please use command line arg '{ClientMainMenuState.AuthProfileCommandLineArg} someName' to set a different auth profile.\n");
            }
        }

        PopupPanel.ShowPopupPanel("Service error", errorMessage);
    }

    private void OnDestroy()
    {
        m_Subscriptions.Dispose();
    }
}
