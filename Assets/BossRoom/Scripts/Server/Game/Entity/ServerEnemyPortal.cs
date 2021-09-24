using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.Samples.BossRoom.Server
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
    [RequireComponent(typeof(NetworkBreakableState))]
    public class ServerEnemyPortal : NetworkBehaviour
    {
        [SerializeField]
        [Tooltip("Portal becomes dormant when ALL of these breakables are broken")]
        public List<NetworkBreakableState> m_BreakableElements;

        [SerializeField]
        [Tooltip("When all breakable elements are broken, wait this long before respawning them (and reactivating)")]
        float m_DormantCooldown;

        // cached reference to our components
        ServerWaveSpawner m_WaveSpawner;
        NetworkBreakableState m_State;

        // currently active "wait X seconds and then restart" coroutine
        Coroutine m_CoroDormant;

        private void Awake()
        {
            m_WaveSpawner = GetComponent<ServerWaveSpawner>();
            m_State = GetComponent<NetworkBreakableState>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            foreach (var breakable in m_BreakableElements)
            {
                breakable.IsBroken.OnValueChanged += OnBreakableBroken;
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
                    breakable.IsBroken.OnValueChanged -= OnBreakableBroken;
            }
        }

        private void OnBreakableBroken(bool wasBroken, bool isBroken)
        {
            if (!wasBroken && isBroken)
                MaintainState();
        }

        private void MaintainState()
        {
            bool hasUnbrokenBreakables = false;
            foreach (var breakable in m_BreakableElements)
            {
                if (breakable && !breakable.IsBroken.Value)
                {
                    hasUnbrokenBreakables = true;
                    break;
                }
            }

            m_State.IsBroken.Value = !hasUnbrokenBreakables;
            m_WaveSpawner.SetSpawnerEnabled(hasUnbrokenBreakables);
            if (!hasUnbrokenBreakables && m_CoroDormant == null)
            {
                m_CoroDormant = StartCoroutine(CoroGoDormantAndThenRestart());
            }
        }

        IEnumerator CoroGoDormantAndThenRestart()
        {
            yield return new WaitForSeconds(m_DormantCooldown);

            foreach (var state in m_BreakableElements)
            {
                if (state)
                {
                    var serverComponent = state.GetComponent<ServerBreakableLogic>();
                    Assert.IsNotNull(serverComponent);
                    serverComponent.Unbreak();
                }
            }
            m_State.IsBroken.Value = false;
            m_WaveSpawner.SetSpawnerEnabled(true);
            m_CoroDormant = null;
        }
    }


}
