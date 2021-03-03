using MLAPI.NetworkedVar;

namespace BossRoom
{
    /// <summary>
    /// Shared Network logic for breakable items. 
    /// </summary>
    public class NetworkBreakableState : MLAPI.NetworkedBehaviour
    {
        /// <summary>
        /// Is the item broken or not?
        /// </summary>
        public NetworkedVarBool IsBroken;
    }

}

