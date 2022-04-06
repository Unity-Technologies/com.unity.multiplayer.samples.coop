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

        public async Task SignInAsync(InitializationOptions initializationOptions)
        {
            string currentTrace = Environment.StackTrace; // For debugging. If we don't get the calling context here, it's lost once the async operation begins.

            try
            {
                await Unity.Services.Core.UnityServices.InitializeAsync(initializationOptions);

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"AsyncRequest threw an exception. Call stack before async call:\n{currentTrace}\n");
                Debug.LogException(e);
                var reason = $"{e.Message} ({e.InnerException?.Message})";
                m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Authentication Error", reason, UnityServiceErrorMessage.Service.Authentication, e));
                throw;
            }


        }
    }
}
