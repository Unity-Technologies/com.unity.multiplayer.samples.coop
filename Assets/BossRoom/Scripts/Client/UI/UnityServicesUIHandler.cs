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
                        switch (exception.Reason)
                        {
                            case LobbyExceptionReason.ValidationError:
                                // Validation check failed on Lobby e.g. in the case of a failed player id match.
                                // Log, not popup
                                break;
                            case LobbyExceptionReason.LobbyConflict:
                                // LobbyConflict can have multiple causes. Let's add other solutions here if there's other situations that arise for this.
                                Debug.LogError($"Got service error {error.Message} with LobbyConflict. Possible conflict cause: Trying to play with two builds on the " +
                                    $"same machine. Please use command line arg '{ProfileManager.AuthProfileCommandLineArg} someName' to set a different auth profile.\n");
                                PopupPanel.ShowPopupPanel("Failed to join Lobby due to a conflict", "See logs for more details.");
                                break;
                            case LobbyExceptionReason.LobbyNotFound:
                            case LobbyExceptionReason.NoOpenLobbies:
                                PopupPanel.ShowPopupPanel("Failed to join Lobby", "See logs for more details.");
                                break;
                            case LobbyExceptionReason.PlayerNotFound:
                                // Log
                                break;
                            case LobbyExceptionReason.LobbyFull:
                                PopupPanel.ShowPopupPanel("Failed to join Lobby", "Lobby is full.");
                                break;
                            case LobbyExceptionReason.InvalidArgument:
                                // Log
                                break;
                            case LobbyExceptionReason.Unauthorized:
                                PopupPanel.ShowPopupPanel("Lobby error", "Unauthorized.");
                                break;
                            case LobbyExceptionReason.Forbidden:
                            case LobbyExceptionReason.EntityNotFound:
                            case LobbyExceptionReason.MethodNotAllowed:
                            case LobbyExceptionReason.NotAcceptable:
                            case LobbyExceptionReason.ProxyAuthenticationRequired:
                            case LobbyExceptionReason.Conflict: //todo: investigate
                            case LobbyExceptionReason.Gone: //todo: investigate
                            case LobbyExceptionReason.LengthRequired:
                            case LobbyExceptionReason.PreconditionFailed:
                            case LobbyExceptionReason.RequestEntityTooLarge:
                            case LobbyExceptionReason.RequestUriTooLong:
                            case LobbyExceptionReason.UnsupportedMediaType:
                            case LobbyExceptionReason.RangeNotSatisfiable:
                            case LobbyExceptionReason.ExpectationFailed: //todo investigate diff between precond and expect fail and FailedDependency and PreconditionRequired
                            case LobbyExceptionReason.Misdirected:
                            case LobbyExceptionReason.UnprocessableTransaction:
                            case LobbyExceptionReason.FailedDependency:
                            case LobbyExceptionReason.TooEarly:
                            case LobbyExceptionReason.UpgradeRequired:
                            case LobbyExceptionReason.PreconditionRequired:
                            case LobbyExceptionReason.RequestHeaderFieldsTooLarge:
                            case LobbyExceptionReason.UnavailableForLegalReasons:
                                // log
                                break;
                            case LobbyExceptionReason.RequestTimeOut:
                                // popup
                                break;
                            case LobbyExceptionReason.Locked: //todo investigate
                                break;
                            case LobbyExceptionReason.RateLimited:
                                // log and react in UI
                                break;
                            case LobbyExceptionReason.InternalServerError:
                            case LobbyExceptionReason.NotImplemented:
                            case LobbyExceptionReason.BadGateway:
                            case LobbyExceptionReason.ServiceUnavailable:
                            case LobbyExceptionReason.GatewayTimeout:
                            case LobbyExceptionReason.HttpVersionNotSupported:
                            case LobbyExceptionReason.VariantAlsoNegotiates:
                            case LobbyExceptionReason.InsufficientStorage:
                            case LobbyExceptionReason.LoopDetected:
                            case LobbyExceptionReason.NotExtended:
                            case LobbyExceptionReason.NetworkAuthenticationRequired:
                                // something happened on the server
                                // Log, not popup
                                break;
                            case LobbyExceptionReason.NetworkError:
                                // NetworkError is returned when the UnityWebRequest failed with this flag set. See the exception stack trace when this reason is provided for context.
                                // Log, not popup
                                break;
                            case LobbyExceptionReason.Unknown:
                                // Unknown is returned when a unrecognized error code is returned by the service. Check the inner exception to get more information.
                                // Log, not popup
                                break;
                            default:
                                throw new NotImplementedException();
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
