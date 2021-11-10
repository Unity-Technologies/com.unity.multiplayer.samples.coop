using System;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Utility class used to listen for Animation-based events through Mecanim. This will invoke an Action when such
    /// anim event is invoked.
    /// </summary>
    /// <remarks>
    /// This class might in the future be refactored/removed with a Netcode for GameObjects refactor to NetworkAnimator.
    /// </remarks>>
    [RequireComponent(typeof(Animator))]
    public class ClientAnimListener : MonoBehaviour
    {
        public event Action<string> animEventRaised;

        void OnAnimEvent(string id)
        {
            animEventRaised?.Invoke(id);
        }
    }
}
