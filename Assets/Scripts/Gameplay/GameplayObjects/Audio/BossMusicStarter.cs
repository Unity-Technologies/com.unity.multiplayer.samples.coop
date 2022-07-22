using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Simple class to restart game theme on main menu load
    /// </summary>
    [RequireComponent(typeof(NetworkLifeState)), RequireComponent(typeof(NetworkHealthState))]
    public class BossMusicStarter : MonoBehaviour
    {
        [SerializeField]
        NetworkLifeState m_NetworkLifeState;

        [SerializeField]
        NetworkHealthState m_NetworkHealthState;

        bool m_Won;

        NetcodeHooks m_NetcodeHooks;

        void Awake()
        {
            enabled = false;
            m_NetcodeHooks = GetComponent<NetcodeHooks>();
            m_NetcodeHooks.OnNetworkSpawnHook += OnSpawn;
        }

        void OnSpawn()
        {
            if (NetworkManager.Singleton.IsClient)
            {
                enabled = true;
                Assert.IsNotNull(m_NetworkLifeState, "NetworkLifeState not set!");
                Assert.IsNotNull(m_NetworkHealthState, "NetworkHealthState not set!");

                m_NetworkLifeState.LifeState.OnValueChanged += OnLifeStateChanged;
                m_NetworkHealthState.HitPoints.OnValueChanged += OnHealthChanged;
            }
        }

        void OnDestroy()
        {
            m_NetcodeHooks.OnNetworkSpawnHook -= OnSpawn;

            var netState = GetComponent<NetworkCharacterState>();
            if (netState != null)
            {
                netState.NetworkLifeState.LifeState.OnValueChanged -= OnLifeStateChanged;
                if (netState.HealthState != null)
                {
                    netState.HealthState.HitPoints.OnValueChanged -= OnHealthChanged;
                }
            }
        }

        private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            if (newValue != LifeState.Alive)
            {
                // players won! Start victory theme
                ClientMusicPlayer.Instance.PlayVictoryMusic();
                m_Won = true;
            }
        }

        private void OnHealthChanged(int previousValue, int newValue)
        {
            // don't do anything if battle is over
            if (m_Won) { return; }

            // make sure battle music started anytime boss is hurt
            if (newValue < previousValue)
            {
                ClientMusicPlayer.Instance.PlayBossMusic();
            }
        }
    }
}
