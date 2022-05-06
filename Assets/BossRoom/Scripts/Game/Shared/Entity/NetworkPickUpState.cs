using Unity.Netcode;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Shared Network logic for targetable, NPC, pickup objects.
    /// </summary>
    public class NetworkPickUpState : NetworkBehaviour, ITargetable
    {
        public bool IsNpc => true;

        public bool IsValidTarget => true;
    }
}
