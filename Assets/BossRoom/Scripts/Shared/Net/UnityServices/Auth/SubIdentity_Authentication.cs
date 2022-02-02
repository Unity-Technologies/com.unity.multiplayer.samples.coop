using System;
using Unity.Services.Authentication;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Auth
{
    /// <summary>
    /// The Authentication package will sign in asynchronously and anonymously. When complete, we will need to store the generated ID.
    /// </summary>
    public class SubIdentity_Authentication : SubIdentity, IDisposable
    {
        private bool m_hasDisposed = false;
        private bool m_needsCleanup = false;

        /// <summary>
        /// This will kick off a login.
        /// </summary>
        public SubIdentity_Authentication()
        {

        }
        ~SubIdentity_Authentication()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (!m_hasDisposed && m_needsCleanup)
            {
                AuthenticationService.Instance.SignedIn -= OnSignInChange;
                AuthenticationService.Instance.SignedOut -= OnSignInChange;
                m_hasDisposed = true;
            }
        }

        public async void DoSignIn(Action onSigninComplete)
        {
            await Unity.Services.Core.UnityServices.InitializeAsync();
            AuthenticationService.Instance.SignedIn += OnSignInChange;
            AuthenticationService.Instance.SignedOut += OnSignInChange;
            m_needsCleanup = true;

            try
            {   if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync(); // Don't sign out later, since that changes the anonymous token, which would prevent the player from exiting lobbies they're already in.
                onSigninComplete?.Invoke();
            }
            catch
            {   UnityEngine.Debug.LogError("Failed to login. Did you remember to set your Project ID under Services > General Settings?");
                throw;
            }

            // Note: If for some reason your login state gets weird, you can comment out the previous block and instead call AuthenticationService.Instance.SignOut().
            // Then, running Play mode will fail to actually function and instead will log out of your previous anonymous account.
            // When you revert that change and run Play mode again, you should be logged in as a new anonymous account with a new default name.
        }

        private void OnSignInChange()
        {
            SetContent("id", AuthenticationService.Instance.PlayerId);
        }
    }
}
