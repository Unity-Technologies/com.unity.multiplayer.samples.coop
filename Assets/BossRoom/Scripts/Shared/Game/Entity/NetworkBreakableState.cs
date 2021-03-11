using MLAPI;
using MLAPI.NetworkVariable;

namespace BossRoom
{
    /// <summary>
    /// Shared Network logic for breakable items.
    /// </summary>
    public class NetworkBreakableState : NetworkBehaviour
    {
        /// <summary>
        /// Is the item broken or not?
        /// </summary>
        public NetworkVariableBool IsBroken;
    }

}

