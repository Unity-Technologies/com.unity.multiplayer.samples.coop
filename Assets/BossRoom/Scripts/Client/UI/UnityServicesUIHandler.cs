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
            var exception = (LobbyServiceException) error.OriginalException;
            if (exception != null)
            {
                switch (exception.Reason)
                {
                    // If the error is one of the following, the player needs to know about it, so show in a popup message. Otherwise, the log in the console is sufficient.
                    case LobbyExceptionReason.LobbyConflict:
                        // LobbyConflict can have multiple causes. Let's add other solutions here if there's other situations that arise for this.
                        Debug.LogError($"Got service error {error.Message} with LobbyConflict. Possible conflict cause: Trying to play with two builds on the " +
                            $"same machine. Please use command line arg '{ProfileManager.AuthProfileCommandLineArg} someName' to set a different auth profile.\n");
                        PopupPanel.ShowPopupPanel("Failed to join Lobby due to a conflict", "See logs for more details.");
                        break;
                    case LobbyExceptionReason.LobbyNotFound:
                        PopupPanel.ShowPopupPanel("Lobby Not Found", "Requested lobby not found. See logs for details.");
                        break;
                    case LobbyExceptionReason.NoOpenLobbies:
                        PopupPanel.ShowPopupPanel("Failed to join Lobby", "No accessible lobbies are currently available for quick-join.");
                        break;
                    case LobbyExceptionReason.LobbyFull:
                        PopupPanel.ShowPopupPanel("Failed to join Lobby", "Lobby is full and can't accept more players.");
                        break;
                    case LobbyExceptionReason.Unauthorized:
                        PopupPanel.ShowPopupPanel("Lobby error", "Unauthorized.");
                        break;
                    case LobbyExceptionReason.RequestTimeOut:
                        PopupPanel.ShowPopupPanel("Lobby error", "Request timed out.");
                        break;
                    case LobbyExceptionReason.BadRequest:
                        PopupPanel.ShowPopupPanel("Lobby error", "Received HTTP error 400 Bad Request from Lobby Service. Is the join code correctly formatted?");
                        break;
                }
            }
        }

        void OnDestroy()
        {
            m_Subscriptions.Dispose();
        }
    }
}
