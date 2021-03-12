using MLAPI;
using MLAPI.NetworkVariable;

namespace BossRoom
{
    /// <summary>
    /// Shared Network logic for breakable items.
    /// </summary>
    public class NetworkBreakableState : NetworkBehaviour, ITargetable
    {
        /// <summary>
        /// Is the item broken or not?
        /// </summary>
        public NetworkVariableBool IsBroken;

        public bool IsNpc { get { return true;  } }

        public bool IsValidTarget {  get { return !IsBroken.Value; } }
    }

}

