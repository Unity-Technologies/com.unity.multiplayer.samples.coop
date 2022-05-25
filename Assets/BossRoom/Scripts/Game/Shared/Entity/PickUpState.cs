using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Shared Network logic for targetable, NPC, pickup objects.
    /// </summary>
    public class PickUpState : MonoBehaviour, ITargetable
    {
        public bool IsNpc => true;

        public bool IsValidTarget => true;
    }
}
