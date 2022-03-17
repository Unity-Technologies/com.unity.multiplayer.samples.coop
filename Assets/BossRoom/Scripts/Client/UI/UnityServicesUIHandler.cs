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
                    var exception = (LobbyServiceException) error.OriginalException;
                    if (exception != null)
                    {
                        // If the error is one of the following, the player needs to know about it, so show in a popup message. Otherwise, the log in the console is sufficient.
                        if (exception.Reason == LobbyExceptionReason.LobbyConflict)
                        {
                            // LobbyConflict can have multiple causes. Let's add other solutions here if there's other situations that arise for this.
                            Debug.LogError($"Got service error {error.Message} with LobbyConflict. Possible conflict cause: Trying to play with two builds on the " +
                                $"same machine. Please use command line arg '{ProfileManager.AuthProfileCommandLineArg} someName' to set a different auth profile.\n");
                            PopupPanel.ShowPopupPanel("Failed to join Lobby due to a conflict", "See logs for more details.");
                        }
                        else if (exception.Reason == LobbyExceptionReason.LobbyNotFound)
                        {
                            PopupPanel.ShowPopupPanel("Lobby Not Found", "Requested lobby not found. See logs for details.");
                        }
                        else if (exception.Reason == LobbyExceptionReason.NoOpenLobbies)
                        {
                            PopupPanel.ShowPopupPanel("Failed to join Lobby", "No accessible lobbies are currently available for quick-join.");
                        }
                        else if (exception.Reason == LobbyExceptionReason.LobbyFull)
                        {
                            PopupPanel.ShowPopupPanel("Failed to join Lobby", "Lobby is full.");
                        }
                        else if (exception.Reason == LobbyExceptionReason.Unauthorized)
                        {
                            PopupPanel.ShowPopupPanel("Lobby error", "Unauthorized.");
                        }
                        else if (exception.Reason == LobbyExceptionReason.RequestTimeOut)
                        {
                            PopupPanel.ShowPopupPanel("Lobby error", "Request timed out.");
                        }
                        else if (exception.Reason == LobbyExceptionReason.BadRequest)
                        {
                            PopupPanel.ShowPopupPanel("Lobby error", "Received HTTP error 400 Bad Request from Lobby Service. Is the join code correctly formatted?");
                        }
                    }
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
            		PopupPanel.ShowPopupPanel("Service error: "+error.Title, errorMessage);
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
