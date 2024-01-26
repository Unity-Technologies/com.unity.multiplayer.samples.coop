using System;

namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    /// <summary>
    /// Entities that are Targetable by Skills should have their shared NetworkState component implement this interface.
    /// </summary>
    public interface ITargetable
    {
        /// <summary>
        /// Is this targetable entity an Npc or a Pc?
        /// </summary>
        bool IsNpc { get; }

        /// <summary>
        /// Is this Target currently valid.
        /// </summary>
        bool IsValidTarget { get; }
    }

}

