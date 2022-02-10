using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Auth
{
    public class AuthenticationAPIInterface
    {
        private IPublisher<UnityServiceErrorMessage> m_UnityServiceErrorMessagePublisher;

        [Inject]
        private void InjectDependencies(IPublisher<UnityServiceErrorMessage> unityServiceErrorMessagePublisher)
        {
            m_UnityServiceErrorMessagePublisher = unityServiceErrorMessagePublisher;
        }

        private void ParseServiceException(AuthenticationException e)
        {
            Debug.LogWarning(e.Message);

            var reason = $"{e.Message} ({e.InnerException?.Message})"; // Lobby error type, then HTTP error type.

            m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Authentication Error", reason));
        }

        public void DoSignInAsync(Action onSigninComplete, Action onFailed, InitializationOptions initializationOptions)
        {
            var task = DoSignIn(initializationOptions);
            UnityServiceCallsTaskWrapper.RunTask<AuthenticationException>(task, onSigninComplete, onFailed, ParseServiceException);
        }

        private async Task DoSignIn(InitializationOptions initializationOptions)
        {
            await Unity.Services.Core.UnityServices.InitializeAsync(initializationOptions);

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
    }
}
