using MLAPI;
using MLAPI.NetworkedVar;

namespace BossRoom
{
    /// <summary>
    /// The data component for the PostGame state object. 
    /// </summary>
    public class PostGameData : NetworkedBehaviour
    {
        /// <summary>
        /// We use a tristate for the GameWon Banner to simplify the display logic on the client. Before the
        /// PostGameState gets its update you'd like to display no banner, rather than either won or lost. Having
        /// a tristate lets us do that in a simple way.
        /// </summary>
        public enum BannerState
        {
            Unset,
            Won,
            Lost
        }

        /// <summary>
        /// When this is true, the players have defeated the Boss and deserve a victory message
        /// </summary>
        public NetworkedVarByte GameBannerState { get; } = new NetworkedVarByte((byte)BannerState.Unset);
    }
}
