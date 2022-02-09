using System;
using System.Collections.Generic;
using Unity.Services.Core;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Auth
{
    /// <summary>
    /// Our internal representation of the local player's credentials, wrapping the data required for interfacing with the identities of that player in the services.
    /// (In use here, it just wraps Auth, but it can be used to combine multiple sets of credentials into one concept of a player.)
    /// </summary>
    public class Identity : IIdentity, IDisposable
    {
        private Dictionary<IIdentityType, SubIdentity> m_subIdentities = new Dictionary<IIdentityType, SubIdentity>();

        public Identity()
        {
            m_subIdentities.Add(IIdentityType.Local, new SubIdentity());
            m_subIdentities.Add(IIdentityType.Auth, new SubIdentity_Authentication());
        }

        public SubIdentity GetSubIdentity(IIdentityType identityType)
        {
            return m_subIdentities[identityType];
        }

        public void OnReProvided(IIdentity prev)
        {
            if (prev is Identity)
            {
                Identity prevIdentity = prev as Identity;
                foreach (var entry in prevIdentity.m_subIdentities)
                    m_subIdentities.Add(entry.Key, entry.Value);
            }
        }

        public void Dispose()
        {
            foreach (var sub in m_subIdentities)
                if (sub.Value is IDisposable)
                    (sub.Value as IDisposable).Dispose();
        }

        public void DoAuthSignIn(Action callbackOnAuthLogin, InitializationOptions initializationOptions)
        {
            if (m_subIdentities.TryGetValue(IIdentityType.Auth, out var identity))
            {
                var authIdentity = (SubIdentity_Authentication) identity;
                authIdentity.DoSignIn(callbackOnAuthLogin, initializationOptions);
            }
        }
    }
}
