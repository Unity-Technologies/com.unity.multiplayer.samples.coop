using System;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using VContainer;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Auth
{
    public class AuthenticationServiceFacade
    {
        [Inject] IPublisher<UnityServiceErrorMessage> m_UnityServiceErrorMessagePublisher;

        public async Task InitializeAndSignInAsync(InitializationOptions initializationOptions)
        {
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
                var reason = $"{e.Message} ({e.InnerException?.Message})";
                m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Authentication Error", reason, UnityServiceErrorMessage.Service.Authentication, e));
                throw;
            }
        }

        public async Task SwitchProfileAndReSignInAsync(string profile)
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }
            AuthenticationService.Instance.SwitchProfile(profile);

            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (Exception e)
            {
                var reason = $"{e.Message} ({e.InnerException?.Message})";
                m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Authentication Error", reason, UnityServiceErrorMessage.Service.Authentication, e));
                throw;
            }
        }

        public async Task<bool> EnsurePlayerIsAuthorized()
        {
            if (AuthenticationService.Instance.IsAuthorized)
            {
                return true;
            }

            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                return true;
            }
            catch (AuthenticationException e)
            {
                var reason = $"{e.Message} ({e.InnerException?.Message})";
                m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Authentication Error", reason, UnityServiceErrorMessage.Service.Authentication, e));
                //not rethrowing for authentication exceptions - any failure to authenticate is considered "handled failure"
                return false;
            }
            catch (Exception e)
            {
                //all other exceptions should still bubble up as unhandled ones
                var reason = $"{e.Message} ({e.InnerException?.Message})";
                m_UnityServiceErrorMessagePublisher.Publish(new UnityServiceErrorMessage("Authentication Error", reason, UnityServiceErrorMessage.Service.Authentication, e));
                throw;
            }
        }

    }
}
