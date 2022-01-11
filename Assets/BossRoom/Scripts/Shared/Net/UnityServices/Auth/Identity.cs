using System;
using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Auth
{
    /// <summary>
    /// Represents some provider of credentials.
    /// Each provider will have its own identity needs, so we'll allow each to define whatever parameters it needs.
    /// Anything that accesses the contents should know what it's looking for.
    /// </summary>
    public class SubIdentity : Observed<SubIdentity>
    {
        protected Dictionary<string, string> m_contents = new Dictionary<string, string>();

        public string GetContent(string key)
        {
            if (!m_contents.ContainsKey(key))
                m_contents.Add(key, null); // Not alerting observers via OnChanged until the value is actually present (especially since this could be called by an observer, which would be cyclical).
            return m_contents[key];
        }

        public void SetContent(string key, string value)
        {
            if (!m_contents.ContainsKey(key))
                m_contents.Add(key, value);
            else
                m_contents[key] = value;
            OnChanged(this);
        }

        public override void CopyObserved(SubIdentity oldObserved)
        {
            m_contents = oldObserved.m_contents;
        }
    }

    public enum IIdentityType { Local = 0, Auth }

    public interface IIdentity
    {
        SubIdentity GetSubIdentity(IIdentityType identityType);
    }

    public class IdentityNoop : IIdentity
    {
        public SubIdentity GetSubIdentity(IIdentityType identityType) { return null; }
        public void OnReProvided(IIdentity other) { }
    }

    /// <summary>
    /// Our internal representation of the local player's credentials, wrapping the data required for interfacing with the identities of that player in the services.
    /// (In use here, it just wraps Auth, but it can be used to combine multiple sets of credentials into one concept of a player.)
    /// </summary>
    public class Identity : IIdentity, IDisposable
    {
        private Dictionary<IIdentityType, SubIdentity> m_subIdentities = new Dictionary<IIdentityType, SubIdentity>();

        public Identity(Action callbackOnAuthLogin)
        {
            m_subIdentities.Add(IIdentityType.Local, new SubIdentity());
            m_subIdentities.Add(IIdentityType.Auth, new SubIdentity_Authentication(callbackOnAuthLogin));
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
    }
}
