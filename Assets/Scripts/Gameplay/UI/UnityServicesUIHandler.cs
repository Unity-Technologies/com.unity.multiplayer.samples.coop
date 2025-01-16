using System;
using Unity.BossRoom.Infrastructure;
using Unity.BossRoom.UnityServices;
using Unity.BossRoom.Utils;
using Unity.Services.Multiplayer;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    public class UnityServicesUIHandler : MonoBehaviour
    {
        ISubscriber<UnityServiceErrorMessage> m_ServiceErrorSubscription;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        [Inject]
        void Initialize(ISubscriber<UnityServiceErrorMessage> serviceError)
        {
            m_ServiceErrorSubscription = serviceError;
            m_ServiceErrorSubscription.Subscribe(ServiceErrorHandler);
        }

        void ServiceErrorHandler(UnityServiceErrorMessage error)
        {
            var errorMessage = error.Message;
            switch (error.AffectedService)
            {
                case UnityServiceErrorMessage.Service.Session:
                {
                    HandleSessionError(error);
                    break;
                }
                case UnityServiceErrorMessage.Service.Authentication:
                {
                    PopupManager.ShowPopupPanel(
                        "Authentication Error",
                        $"{error.OriginalException.Message} \n tip: You can still use the Direct IP connection option.");
                    break;
                }
                default:
                {
                    PopupManager.ShowPopupPanel("Service error: " + error.Title, errorMessage);
                    break;
                }
            }
        }

        void HandleSessionError(UnityServiceErrorMessage error)
        {
            if (error.OriginalException is AggregateException { InnerException: SessionException sessionException })
            {
                switch (sessionException.Error)
                {
                    case SessionError.SessionNotFound:
                        PopupManager.ShowPopupPanel("Session Not Found", "Requested Session not found. The join code is incorrect or the Session has ended.");
                        break;
                    case SessionError.NotAuthorized:
                        PopupManager.ShowPopupPanel("Session error", "Received HTTP error 401 Unauthorized from Session Service.");
                        break;
                    case SessionError.MatchmakerAssignmentTimeout: // this can happen when using quick join
                        PopupManager.ShowPopupPanel("Session error", "Received HTTP error 408 Request timed out from Session Service.");
                        break;
                    case SessionError.Unknown:
                    default:
                        PopupManager.ShowPopupPanel("Unknown Error", sessionException.Message);
                        break;
                }
            }
        }

        void OnDestroy()
        {
            if (m_ServiceErrorSubscription != null)
            {
                m_ServiceErrorSubscription.Unsubscribe(ServiceErrorHandler);
            }
        }
    }
}
