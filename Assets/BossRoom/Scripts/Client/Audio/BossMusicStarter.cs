using UnityEngine;


namespace BossRoom.Client
{
    /// <summary>
    /// Simple class to restart game theme on main menu load
    /// </summary>
    public class BossMusicStarter : MonoBehaviour
    {
        private ClientMusicPlayer m_MusicPlayer;

        void Start()
        {
            GameObject musicPlayerObj = GameObject.FindGameObjectWithTag("MusicPlayer");
            m_MusicPlayer = musicPlayerObj.GetComponent<ClientMusicPlayer>();

            var netState = GetComponent<NetworkCharacterState>();
            netState.NetworkLifeState.OnValueChanged += OnLifeStateChanged;
            netState.HealthState.HitPoints.OnValueChanged += OnHealthChanged;
        }

        private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            if (newValue!= LifeState.Alive)
            {
                // players won! Start victory theme
                m_MusicPlayer.PlayVictoryMusic();
            }
        }

        private void OnHealthChanged(int previousValue, int newValue)
        {
            // make sure battle music started anytime boss is hurt
            if (previousValue>newValue)
            {
                m_MusicPlayer.PlayBossMusic();
            }
        }
    }
}
