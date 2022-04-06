using System;
using Unity.Multiplayer.Samples.BossRoom.Shared;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure;
using Unity.Services.Lobbies;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    public class UnityServicesUIHandler : MonoBehaviour
    {
        IDisposable m_Subscriptions;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        [Inject]
        void Initialize(ISubscriber<UnityServiceErrorMessage> serviceError)
        {
            m_Subscriptions = serviceError.Subscribe(ServiceErrorHandler);
        }

        void ServiceErrorHandler(UnityServiceErrorMessage error)
        {
            var errorMessage = error.Message;
            switch (error.AffectedService)
            {
                case UnityServiceErrorMessage.Service.Lobby:
                {
                    HandleLobbyError(error);
                    break;
                }
                case UnityServiceErrorMessage.Service.Authentication:
                {
                    PopupPanel.ShowPopupPanel(
                        "Authentication Error",
                        $"{error.OriginalException.Message} \n tip: You can still use the Direct IP connection option.");
                    break;
                }
                default:
                {
                    PopupPanel.ShowPopupPanel("Service error: " + error.Title, errorMessage);
                    break;
                }
            }
        }

        void HandleLobbyError(UnityServiceErrorMessage error)
        {
            var errorMessage = error.Message;
            switch (((LobbyServiceException)error.OriginalException).Reason)
            {
                case LobbyExceptionReason.LobbyConflict:
                {
                    errorMessage += "\nSee logs for possible causes and solution.";
                    Debug.LogError($"Got service error {error.Message} with LobbyConflict. Possible conflict cause: Trying to play with two builds on the " +
                        $"same machine. Please use command line arg '{ProfileManager.AuthProfileCommandLineArg} someName' to set a different auth profile.\n");
                    break;
                }
                case LobbyExceptionReason.LobbyFull:
                {
                    PopupPanel.ShowPopupPanel("Failed to join lobby", "Lobby is full and can't accept more players");
                    // Returning out of the function because we replace default popup panel with this.
                    return;
                }
            }

            PopupPanel.ShowPopupPanel("Service error: " + error.Title, errorMessage);
        }

        void OnDestroy()
        {
            m_Subscriptions?.Dispose();
        }
    }
}
