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

        async Task<T> ExceptionHandling<T>(Task<T> task)
        {
            string currentTrace = Environment.StackTrace; // For debugging. If we don't get the calling context here, it's lost once the async operation begins.

            try
            {
                return await task;
            }
            catch (Exception e)
            {
                OnServiceException(e);
                Debug.LogError($"AsyncRequest threw an exception. Call stack before async call:\n{currentTrace}\n");
                throw;
            }
        }

        async Task ExceptionHandling(Task task)
        {
            string currentTrace = Environment.StackTrace; // For debugging. If we don't get the calling context here, it's lost once the async operation begins.

            try
            {
                await task;
            }
            catch (Exception e)
            {
                OnServiceException(e);
                Debug.LogError($"AsyncRequest threw an exception. Call stack before async call:\n{currentTrace}\n");
                throw;
            }
        }

        void OnServiceException(Exception e)
        {
            Debug.LogException(e);
            var reason = $"{e.Message} ({e.InnerException?.Message})";
            m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Authentication Error", reason, UnityServiceErrorMessage.Service.Authentication, e));
        }

        public async Task DoSignInAsync(InitializationOptions initializationOptions)
        {
            await ExceptionHandling(TrySignIn(initializationOptions));
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
