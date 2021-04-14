using MLAPI;
using MLAPI.Spawning;
using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// Visualization of a DashAttackAction.
    /// </summary>
    public class DashAttackActionFX : ActionFX
    {
        /// <summary>
        /// Amount of time we will be dashing. 0 = sentinel value meaning "not set yet"
        /// </summary>
        private float m_DashDuration = 0;

        public DashAttackActionFX(ref ActionRequestData data, ClientCharacterVisualization parent) : base(ref data, parent) { }

        public override bool Start()
        {
            if (!Anticipated)
            {
                PlayStartAnim();
            }

            base.Start();
            return true;
        }

        private void PlayStartAnim()
        {
            m_Parent.OurAnimator.SetTrigger(Description.Anim);
        }

        public override void AnticipateAction()
        {
            base.AnticipateAction();
            PlayStartAnim();
        }

        public override bool Update()
        {
            if (TimeRunning >= Description.ExecTimeSeconds && m_DashDuration == 0)
            {
                // we've waited for ExecTime, so how long will we be dashing?
                // (we calculate this as late as possible, after ExecTime has elapsed,
                // so that our notion of the target's position is as up-to-date as possible)
                m_DashDuration = GetDashDuration();
            }
            return m_DashDuration == 0 || TimeRunning < Description.ExecTimeSeconds + m_DashDuration;
        }

        public override void Cancel()
        {
            if (!string.IsNullOrEmpty(Description.Anim2))
            {
                m_Parent.OurAnimator.SetTrigger(Description.Anim2);
            }
        }

        private float GetDashDuration()
        {
            var targetPosition = Data.Position;
            if (Data.TargetIds != null && Data.TargetIds.Length > 0)
            {
                ulong targetId = Data.TargetIds[0];
                var targetObject = NetworkSpawnManager.SpawnedObjects[targetId];
                if (targetObject)
                {
                    targetPosition = targetObject.transform.position;
                }
            }

            float distanceToTargetPos = Vector3.Distance(targetPosition, m_Parent.transform.position);
            return distanceToTargetPos / Description.MoveSpeed;
        }

    }
}
