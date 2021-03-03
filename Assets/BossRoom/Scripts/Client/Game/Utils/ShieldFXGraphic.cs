using UnityEngine;

namespace BossRoom.Visual
{
    public class ShieldFXGraphic : SpecialFXGraphic
    {
        /// <summary>
        /// For the shield we do custom behaviour when our charge ends
        /// </summary>
        public override void EndCharge()
        {
            // TODO: end some particles but some will play after charge is ended
        }
    }
}
