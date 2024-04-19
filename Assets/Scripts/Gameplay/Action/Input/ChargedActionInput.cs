using UnityEngine;

namespace Unity.BossRoom.Gameplay.Actions
{
    public class ChargedActionInput : BaseActionInput
    {
        protected float m_StartTime;

        private void Start()
        {
            // get our particle near the right spot!
            transform.position = m_Origin;

            m_StartTime = Time.time;
            // right now we only support "untargeted" charged attacks.
            // Will need more input (e.g. click position) for fancier types of charged attacks!
            var data = new ActionRequestData
            {
                Position = transform.position,
                ActionID = m_ActionPrototypeID,
                ShouldQueue = false,
                TargetIds = null
            };
            m_SendInput(data);
        }

        public override void OnReleaseKey()
        {
            m_PlayerOwner.ServerStopChargingUpRpc();
            Destroy(gameObject);
        }

    }
}
