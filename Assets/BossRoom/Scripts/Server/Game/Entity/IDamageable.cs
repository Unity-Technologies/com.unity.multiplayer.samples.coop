using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// Generic interface for damageable objects in the game. This includes ServerCharacter, as well as other things like
    /// ServerBreakableLogic.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Receives HP damage or healing.
        /// </summary>
        /// <param name="inflicter">The Character responsible for the damage. May be null.</param>
        /// <param name="HP">The damage done. Positive value is damage, negative is healing.</param>
        void ReceiveHP(ServerCharacter inflicter, int HP);

        /// <summary>
        /// The NetworkId of this object.
        /// </summary>
        ulong NetworkObjectId { get; }

        /// <summary>
        /// The transform of this object.
        /// </summary>
        Transform transform { get; }
    }
}

