using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects
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
        /// <param name="HP">The damage done. Negative value is damage, positive is healing.</param>
        void ReceiveHP(ServerCharacter inflicter, int HP);

        /// <summary>
        /// The NetworkId of this object.
        /// </summary>
        ulong NetworkObjectId { get; }

        /// <summary>
        /// The transform of this object.
        /// </summary>
        Transform transform { get; }

        [Flags]
        public enum SpecialDamageFlags
        {
            None = 0,
            UnusedFlag = 1 << 0, // does nothing; see comments below
            StunOnTrample = 1 << 1,
            NotDamagedByPlayers = 1 << 2,

            // The "UnusedFlag" flag does nothing. It exists to work around a Unity editor quirk involving [Flags] enums:
            // if you enable all the flags, Unity stores the value as 0xffffffff (labeled "Everything"), meaning that not
            // only are all the currently-existing flags enabled, but any future flags you added later would also be enabled!
            // This is not future-proof and can cause hard-to-track-down problems, when prefabs magically inherit a new flag
            // you just added. So we have the Unused flag, which should NOT do anything, and shouldn't be selected on prefabs.
            // It's just there so that we can select all the "real" flags and not get it turned into "Everything" in the editor.
        }
        SpecialDamageFlags GetSpecialDamageFlags();

        /// <summary>
        /// Are we still able to take damage? If we're broken or dead, should return false!
        /// </summary>
        bool IsDamageable();
    }
}

