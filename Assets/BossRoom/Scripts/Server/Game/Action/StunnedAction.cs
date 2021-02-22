using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// Action that plays while a character is Stunned. The character does nothing... just sits there.
    /// 
    /// If desired, we can make the character take extra damage from attacks while stunned!
    /// The 'Amount' field of our ActionDescription is used as a multiplier on damage suffered.
    /// (Set it to 1 if you don't want to take more damage while stunned... set it to 2 to take double damage,
    /// or 0.5 to take half damage, etc.)
    /// </summary>
    public class StunnedAction : Action
    {
        public StunnedAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data)
        {
        }

        public override bool Start()
        {
            m_Parent.NetState.RecvDoActionClientRPC(Data);
            return true;
        }

        public override bool Update()
        {
            return true;
        }

        public override void BuffValue(BuffableValue buffType, ref float buffedValue)
        {
            if (buffType == BuffableValue.PercentDamageReceived)
            {
                buffedValue *= Description.Amount;
            }
        }

    }
}
