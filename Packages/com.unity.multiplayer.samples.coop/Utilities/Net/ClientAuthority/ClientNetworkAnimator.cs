using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Multiplayer.Samples.Utilities.ClientAuthority
{
    /// <summary>
    /// Used for syncing an animator with client side changes. This includes host. Pure server as owner isn't supported
    /// by this. Please use NetworkAnimator for animations that'll always be owned by the server.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientNetworkAnimator : NetworkAnimator
    {
        /// <summary>
        /// Used to determine who can write to this animator. Owner client only.
        /// This imposes state to the server. This is putting trust on your clients. Make sure no security-sensitive features use this animator.
        /// </summary>
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
