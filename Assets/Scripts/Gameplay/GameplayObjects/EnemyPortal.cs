using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    /// <summary>
    /// ServerEnemyPortal is a stationary dungeon element that spawns monsters when a player is
    /// nearby. It has one or more "breakable bits". When all the breakable elements are broken,
    /// the portal becomes dormant for a fixed amount of time, before repairing its breakables
    /// and starting up again.
    ///
    /// The actual monster-spawning logic is managed by a ServerWaveSpawner component.
    /// </summary>
    /// <remarks>
    /// The ServerEnemyPortal also has its own NetworkBreakableState. This is for the graphics of
    /// the portal itself (a glowy visual effect stops when Broken, turns back on when unbroken)
    /// </remarks>
    [RequireComponent(typeof(ServerWaveSpawner))]
    public class EnemyPortal : NetworkBehaviour, ITargetable
    {
        [SerializeField]
        [Tooltip("Portal becomes dormant when ALL of these breakables are broken")]
        public List<Breakable> m_BreakableElements;

        [SerializeField]
        [Tooltip("When all breakable elements are broken, wait this long before respawning them (and reactivating)")]
        float m_DormantCooldown;

        [SerializeField]
        Breakable m_Breakable;

        public bool IsNpc => true;

        public bool IsValidTarget => !m_Breakable.IsBroken;

        // cached reference to our components
        [SerializeField]
        ServerWaveSpawner m_WaveSpawner;

        // currently active "wait X seconds and then restart" coroutine
        Coroutine m_CoroDormant;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            foreach (var breakable in m_BreakableElements)
            {
                breakable.OnBroken += OnBreakableBroken;
            }

            MaintainState();
        }

        public override void OnNetworkDespawn()
        {
            if (m_CoroDormant != null)
                StopCoroutine(m_CoroDormant);

            foreach (var breakable in m_BreakableElements)
            {
                if (breakable)
                {
                    breakable.OnBroken -= OnBreakableBroken;
                }
            }
        }

        void OnBreakableBroken()
        {
            MaintainState();
        }

        void MaintainState()
        {
            bool hasUnbrokenBreakables = false;
            foreach (var breakable in m_BreakableElements)
            {
                if (breakable && !breakable.IsBroken)
                {
                    hasUnbrokenBreakables = true;
                    break;
                }
            }

            if (!hasUnbrokenBreakables)
            {
                m_Breakable.Break();
            }
            else
            {
                m_Breakable.Unbreak();
            }
            m_WaveSpawner.SetSpawnerEnabled(hasUnbrokenBreakables);
            if (!hasUnbrokenBreakables && m_CoroDormant == null)
            {
                m_CoroDormant = StartCoroutine(CoroGoDormantAndThenRestart());
            }
        }

        IEnumerator CoroGoDormantAndThenRestart()
        {
            yield return new WaitForSeconds(m_DormantCooldown);

            Restart();
        }

        void Restart()
        {
            foreach (var state in m_BreakableElements)
            {
                if (state)
                {
                    var serverComponent = state.GetComponent<Breakable>();
                    Assert.IsNotNull(serverComponent);
                    serverComponent.Unbreak();
                }
            }

            m_Breakable.Unbreak();
            m_WaveSpawner.SetSpawnerEnabled(true);
            m_CoroDormant = null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void ForceRestart()
        {
            if (m_CoroDormant != null)
            {
                StopCoroutine(m_CoroDormant);
            }
            Restart();
        }

        public void ForceDestroy()
        {
            foreach (var state in m_BreakableElements)
            {
                if (state)
                {
                    var serverComponent = state.GetComponent<Breakable>();
                    Assert.IsNotNull(serverComponent);
                    serverComponent.ReceiveHP(null, Int32.MinValue);
                }
            }
        }
#endif
    }


}
