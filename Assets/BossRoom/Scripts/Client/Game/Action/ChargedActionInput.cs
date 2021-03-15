using System.Collections;
using UnityEngine;

namespace BossRoom.Visual
{
    public class ChargedActionInput : BaseActionInput
    {
        protected float m_StartTime;

        private void Start()
        {
            // get our particle near the right spot!
            transform.position = m_PlayerOwner.transform.position;

            m_StartTime = Time.time;
            // right now we only support "untargeted" charged attacks.
            // Will need more input (e.g. click position) for fancier types of charged attacks!
            var data = new ActionRequestData
            {
                Position = transform.position,
                ActionTypeEnum = m_ActionType,
                ShouldQueue = false,
                TargetIds = null
            };
            m_PlayerOwner.RecvDoActionServerRPC(data);
        }

        private void Update()
        {
            // FIXME: this is necessary to ensure the GameObject is destroyed, because clicking on the buttons
            // in the UI only sends one event, onClick. The buttons need to send separate "button down" and
            // "button up" events, like we use for keyboard keypresses, to make it possible to "charge up" an
            // attack via keyboard. (When this is done, OnReleaseKey() will reliably be called, so it can
            // destroy the GameObject, and this entire Update() function can be removed.)
            var description = GameDataSource.Instance.ActionDataByType[m_ActionType];
            if (Time.time - m_StartTime > description.DurationSeconds &&
                Time.time - m_StartTime > description.ExecTimeSeconds) // this check handles theoretical cases where Duration is 0 (indeterminate) but we still have a fixed wind-up time
            {
                Destroy(gameObject);
            }
        }

        public override void OnReleaseKey()
        {
            m_PlayerOwner.RecvStopChargingUpServerRpc();
            Destroy(gameObject);
        }

    }
}
