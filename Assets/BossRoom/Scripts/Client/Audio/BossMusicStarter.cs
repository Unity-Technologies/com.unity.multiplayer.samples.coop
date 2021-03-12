using UnityEngine;


namespace BossRoom.Client
{
    /// <summary>
    /// Simple class to restart game theme on main menu load
    /// </summary>
    public class BossMusicStarter : MonoBehaviour
    {
        private bool m_Won = false;

        void Start()
        {
            var netState = GetComponent<NetworkCharacterState>();
            netState.NetworkLifeState.OnValueChanged += OnLifeStateChanged;
            netState.HealthState.HitPoints.OnValueChanged += OnHealthChanged;
        }

        private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            if (newValue!= LifeState.Alive)
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
            if (previousValue>newValue)
            {
                ClientMusicPlayer.Instance.PlayBossMusic();
            }
        }
    }
}
