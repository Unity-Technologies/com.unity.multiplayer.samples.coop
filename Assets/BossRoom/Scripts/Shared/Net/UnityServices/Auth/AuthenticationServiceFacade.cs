using System;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Auth
{
    public class AuthenticationServiceFacade
    {
        IPublisher<UnityServiceErrorMessage> m_UnityServiceErrorMessagePublisher;

        [Inject]
        void InjectDependencies(IPublisher<UnityServiceErrorMessage> unityServiceErrorMessagePublisher)
        {
            m_UnityServiceErrorMessagePublisher = unityServiceErrorMessagePublisher;
        }

        void OnServiceException(Exception e)
        {
            Debug.LogException(e);
            var reason = $"{e.Message} ({e.InnerException?.Message})";
            m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Authentication Error", reason, UnityServiceErrorMessage.Service.Authentication, e));
        }

        public void DoSignInAsync(Action onSigninComplete, Action onFailed, InitializationOptions initializationOptions)
        {
            var task = TrySignIn(initializationOptions);
            UnityServiceCallsTaskWrapper.RunTaskAsync<AuthenticationException>(task, onSigninComplete, onFailed, OnServiceException);
        }

        async Task TrySignIn(InitializationOptions initializationOptions)
        {
            await Unity.Services.Core.UnityServices.InitializeAsync(initializationOptions);

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
    }
}
