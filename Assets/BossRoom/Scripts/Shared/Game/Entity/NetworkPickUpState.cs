using Unity.Netcode;

namespace BossRoom
{
    /// <summary>
    /// Shared Network logic for breakable items.
    /// </summary>
    public class NetworkPickUpState : NetworkBehaviour, ITargetable
    {
        public bool IsNpc => true;

        public bool IsValidTarget => true;
    }
}
