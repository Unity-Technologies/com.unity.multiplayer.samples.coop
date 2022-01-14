using System.Collections.Generic;
using BossRoom.Scripts.Shared.Infrastructure;

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
}
