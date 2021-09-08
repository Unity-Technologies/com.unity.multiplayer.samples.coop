using MLAPI;

namespace BossRoom
{
    /// <summary>
    /// Shared Network logic for breakable items.
    /// </summary>
    public class NetworkPickupState : NetworkBehaviour, ITargetable
    {
        public bool IsNpc => true;

        public bool IsValidTarget => true;
    }
}
