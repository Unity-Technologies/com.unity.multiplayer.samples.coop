using MLAPI;
using MLAPI.Spawning;
using UnityEngine;

namespace BossRoom.Server
{
    /// <summary>
    /// Causes the character to become hidden to enemies and other players. Notes:
    /// - Stealth starts after the ExecTimeSeconds has elapsed. If they are attacked during the Exec time, stealth is aborted.
    /// - Stealth ends when the player attacks or is damaged.
    /// - 
    /// </summary>
    public class StealthModeAction : Action
    {
        private bool m_IsStealthStarted = false;
        private bool m_IsStealthEnded = false;

        public StealthModeAction(ServerCharacter parent, ref ActionRequestData data) : base(parent, ref data) { }

        public override bool Start()
        {
            Debug.Log("STEALTH BEGIN");
            m_Parent.NetState.RecvDoActionClientRPC(Data);

            // not allowed to walk while going stealthy!
            var movement = m_Parent.GetComponent<ServerCharacterMovement>();
            if (!movement.IsPerformingForcedMovement())
            {
                movement.CancelMove();
            }    
            return true;
        }

        public override bool ShouldBecomeNonBlocking()
        {
            return TimeRunning >= Description.ExecTimeSeconds;
        }

        public override bool Update()
        {
            if (TimeRunning >= Description.ExecTimeSeconds && !m_IsStealthStarted && !m_IsStealthEnded)
            {
                m_IsStealthStarted = true;
                m_Parent.NetState.IsStealthy.Value = 1;
            }
            return !m_IsStealthEnded;
        }

        public override void Cancel()
        {
            Debug.Log("STEALTH END");
            if (m_IsStealthStarted)
            {
                m_Parent.NetState.IsStealthy.Value = 0;
            }
            m_Parent.NetState.RecvCancelActionClientRpc();
        }

        public override void OnGameplayActivity(GameplayActivity activityType)
        {
            // we break stealth after an attack. (Or after being hit, which could happen during exec time before we're stealthed, or even afterwards, such as from an AoE attack)
            if (activityType == GameplayActivity.DamagedEnemy || activityType == GameplayActivity.AttackedByEnemy)
            {
                m_IsStealthEnded = true;
            }
        }

    }
}
