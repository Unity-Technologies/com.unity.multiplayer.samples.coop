using System;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects.AnimationCallbacks
{
    /// <summary>
    /// De-parents the boss's helmet when he is defeated. This prevents the helmet from jittering during the
    /// animation loop. The "boss defeat start" animation has an event that calls OnAnimEvent("HelmetLanded")
    /// right as the helmet hits the ground. That's what we listen for here.
    /// </summary>
    /// <remarks>
    /// Without this code, the boss's helmet appears to "jiggle" a bit while the boss throws his temper-tantrum.
    /// The animation in the FBX keeps the helmet staying stationary, but it moves in-game due to animation
    /// compression and floating-point round off. Since the helmet is parented deep in the transform hierarchy,
    /// it's difficult to keep the helmet precisely still while all of its parent transforms are moving around wildly.
    ///
    /// We could get rid of the majority of the jiggle by disabling animation-compression on the FBX, but that could
    /// adversely impact performance. Since this is a special case, we deal with it via this special-case script.
    /// </remarks>
    public class BossDeathHelmetHandler : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The transform of the boss's helmet, which will become de-parented when the boss is defeated")]
        Transform m_HelmetTransform;

        bool m_HasDeparentedHelmet;

        public void OnAnimEvent(string id)
        {
            if (id == "HelmetLanded" && !m_HasDeparentedHelmet)
            {
                m_HasDeparentedHelmet = true;
                m_HelmetTransform.parent = null;
            }
        }

        public void OnDestroy()
        {
            if (m_HasDeparentedHelmet && m_HelmetTransform)
            {
                // the boss is going away, so the helmet should go too!
                Destroy(m_HelmetTransform.gameObject);
            }
        }

    }
}
