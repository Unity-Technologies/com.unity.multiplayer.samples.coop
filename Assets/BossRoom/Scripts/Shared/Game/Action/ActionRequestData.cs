using System.IO;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// List of all Actions supported in the game.
    /// </summary>
    public enum ActionType
    {
        None,
        TankBaseAttack,
        ArcherBaseAttack,
        MageBaseAttack,
        RogueBaseAttack,
        ImpBaseAttack,
        ImpBossBaseAttack,
        GeneralChase,
        GeneralRevive,

        DriveArrow,
        Emote1,
        Emote2,
        Emote3,
        Emote4,
        TankTestability,
        TankShieldBuff,
        ImpBossTrampleAttack,
        Stunned,
    }


    /// <summary>
    /// List of all Types of Actions. There is a many-to-one mapping of Actions to ActionLogics.
    /// </summary>
    public enum ActionLogic
    {
        Melee,
        RangedTargeted,
        Chase,
        Revive,
        LaunchProjectile,
        Emote,
        RangedFXTargeted,
        AoE,
        Trample,
        ChargedShield,
        Stunned,

        //O__O adding a new ActionLogic branch? Update Action.MakeAction!
    }


    /// <summary>
    /// Retrieves static information about ActionLogics
    /// </summary>
    public static class ActionLogicUtils
    {
        /// <summary>
        /// When requesting an action with this type of ActionLogic, do we need to store the Position where player clicked?
        /// (We separate this out so that we don't need to send the extra Vector3 across the network if unneeded.)
        /// </summary>
        public static bool IsPositionUsed(ActionLogic logic)
        {
            return logic == ActionLogic.RangedFXTargeted;
        }

    }

    /// <summary>
    /// Comprehensive class that contains information needed to play back any action on the server. This is what gets sent client->server when
    /// the Action gets played, and also what gets sent server->client to broadcast the action event. Note that the OUTCOMES of the action effect
    /// don't ride along with this object when it is broadcast to clients; that information is sync'd separately, usually by NetworkedVars.
    /// </summary>
    public struct ActionRequestData : MLAPI.Serialization.IBitWritable
    {
        public ActionType ActionTypeEnum;      //the action to play.
        public Vector3 Position;           //center position of skill, e.g. "ground zero" of a fireball skill.
        public Vector3 Direction;          //direction of skill, if not inferrable from the character's current facing.
        public ulong[] TargetIds;          //networkIds of targets, or null if untargeted.
        public float Amount;               //can mean different things depending on the Action. For a ChaseAction, it will be target range the ChaseAction is trying to achieve.
        public bool ShouldQueue;           //if true, this action should queue. If false, it should clear all current actions and play immediately. 
        public bool ShouldClose;           //if true, the server should synthesize a ChaseAction to close to within range of the target before playing the Action. Ignored for untargeted actions.
        public bool CancelMovement;        // if true, movement is cancelled before playing this action

        //O__O Hey, are you adding something? Be sure to update ActionLogicInfo, as well as the methods below.



        [System.Flags]
        private enum PackFlags
        {
            None = 0,
            HasPosition = 1,
            HasDirection = 1 << 1,
            HasTargetIds = 1 << 2,
            HasAmount = 1 << 3,
            ShouldQueue = 1 << 4,
            ShouldClose = 1 << 5,
            CancelMovement = 1 << 6,
            //currently serialized with a byte. Change Read/Write if you add more than 8 fields.
        }

        /// <summary>
        /// Returns true if the ActionRequestDatas are "functionally equivalent" (not including their Queueing or Closing properties). 
        /// </summary>
        public bool Compare(ref ActionRequestData rhs)
        {
            bool scalarParamsEqual = (ActionTypeEnum, Position, Direction, Amount) == (rhs.ActionTypeEnum, rhs.Position, rhs.Direction, rhs.Amount);
            if (!scalarParamsEqual) { return false; }

            if (TargetIds == rhs.TargetIds) { return true; } //covers case of both being null. 
            if (TargetIds == null || rhs.TargetIds == null || TargetIds.Length != rhs.TargetIds.Length) { return false; }
            for (int i = 0; i < TargetIds.Length; i++)
            {
                if (TargetIds[i] != rhs.TargetIds[i]) { return false; }
            }

            return true;
        }


        private PackFlags GetPackFlags()
        {
            PackFlags flags = PackFlags.None;
            if (Position != Vector3.zero) { flags |= PackFlags.HasPosition; }
            if (Direction != Vector3.zero) { flags |= PackFlags.HasDirection; }
            if (TargetIds != null) { flags |= PackFlags.HasTargetIds; }
            if (Amount != 0) { flags |= PackFlags.HasAmount; }
            if (ShouldQueue) { flags |= PackFlags.ShouldQueue; }
            if (ShouldClose) { flags |= PackFlags.ShouldClose; }
            if (CancelMovement) { flags |= PackFlags.CancelMovement; }


            return flags;
        }

        public void Read(Stream stream)
        {
            using (var reader = MLAPI.Serialization.Pooled.PooledBitReader.Get(stream))
            {
                ActionTypeEnum = (ActionType)reader.ReadInt16();
                PackFlags flags = (PackFlags)reader.ReadByte();

                ShouldClose = (flags & PackFlags.ShouldClose) != 0;
                ShouldQueue = (flags & PackFlags.ShouldQueue) != 0;
                CancelMovement = (flags & PackFlags.CancelMovement) != 0;

                if ((flags & PackFlags.HasPosition) != 0)
                {
                    Position = reader.ReadVector3();
                }
                if ((flags & PackFlags.HasDirection) != 0)
                {
                    Direction = reader.ReadVector3();
                }
                if ((flags & PackFlags.HasTargetIds) != 0)
                {
                    TargetIds = reader.ReadULongArray();
                }
                if ((flags & PackFlags.HasAmount) != 0)
                {
                    Amount = reader.ReadSingle();
                }
            }
        }

        public void Write(Stream stream)
        {
            using (var writer = MLAPI.Serialization.Pooled.PooledBitWriter.Get(stream))
            {
                PackFlags flags = GetPackFlags();

                writer.WriteInt16((short)ActionTypeEnum);
                writer.WriteByte((byte)flags);

                //booleans are implicitly serialized just by setting their respective bit in GetPackFlags.

                if ((flags & PackFlags.HasPosition) != 0)
                {
                    writer.WriteVector3(Position);
                }
                if ((flags & PackFlags.HasDirection) != 0)
                {
                    writer.WriteVector3(Direction);
                }
                if ((flags & PackFlags.HasTargetIds) != 0)
                {
                    writer.WriteULongArray(TargetIds);
                }
                if ((flags & PackFlags.HasAmount) != 0)
                {
                    writer.WriteSingle(Amount);
                }
            }
        }
    }
}

