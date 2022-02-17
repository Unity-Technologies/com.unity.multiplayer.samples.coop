using System;
using System.Threading.Tasks;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Auth
{
    public class AuthenticationServiceFacade
    {
        private IPublisher<UnityServiceErrorMessage> m_UnityServiceErrorMessagePublisher;

        [Inject]
        private void InjectDependencies(IPublisher<UnityServiceErrorMessage> unityServiceErrorMessagePublisher)
        {
            m_UnityServiceErrorMessagePublisher = unityServiceErrorMessagePublisher;
        }

        private void OnServiceException(AuthenticationException e)
        {
            Debug.LogWarning(e.Message);

            var reason = $"{e.Message} ({e.InnerException?.Message})"; // Lobby error type, then HTTP error type.

            m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Authentication Error", reason));
        }

        public void DoSignInAsync(Action onSigninComplete, Action onFailed, InitializationOptions initializationOptions)
        {
            var task = TrySignIn(initializationOptions);
            UnityServiceCallsTaskWrapper.RunTask<AuthenticationException>(task, onSigninComplete, onFailed, OnServiceException);
        }

        private async Task TrySignIn(InitializationOptions initializationOptions)
        {
            await Unity.Services.Core.UnityServices.InitializeAsync(initializationOptions);

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
    }
}
