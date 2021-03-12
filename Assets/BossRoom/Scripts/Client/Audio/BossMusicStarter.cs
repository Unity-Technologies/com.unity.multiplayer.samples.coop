using UnityEngine;


namespace BossRoom.Client
{
    /// <summary>
    /// Simple class to restart game theme on main menu load
    /// </summary>
    public class BossMusicStarter : MonoBehaviour
    {
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
            }
        }

        private void OnHealthChanged(int previousValue, int newValue)
        {
            // make sure battle music started anytime boss is hurt
            if (previousValue>newValue)
            {
                ClientMusicPlayer.Instance.PlayBossMusic();
            }
        }
    }
}
