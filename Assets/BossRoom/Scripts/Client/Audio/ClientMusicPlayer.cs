using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace BossRoom.Client
{
    /// <summary>
    /// Music player that handles start of boss battle, victory and restart
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ClientMusicPlayer : MonoBehaviour
    {
        [SerializeField]
        AudioClip m_ThemeMusic;

        [SerializeField]
        AudioClip m_BossMusic;

        [SerializeField]
        AudioClip m_VictoryMusic;

        [FormerlySerializedAs("m_source")]
        [SerializeField]
        AudioSource m_AudioSource;

        /// <summary>
        /// static accessor for ClientMusicPlayer
        /// </summary>
        public static ClientMusicPlayer Instance { get; private set; }

        public void PlayThemeMusic(bool restart)
        {
            PlayTrack(m_ThemeMusic, true, restart);
        }

        public void PlayBossMusic()
        {
            // this can be called multiple times - play with restart = false
            PlayTrack(m_BossMusic, true, false);
        }

        public void PlayVictoryMusic()
        {
            PlayTrack(m_VictoryMusic, false, false);
        }

        void PlayTrack(AudioClip clip, bool looping, bool restart)
        {
            if (m_AudioSource.isPlaying)
            {
                // if we dont want to restart the clip, do nothing if it is playing
                if (!restart && m_AudioSource.clip==clip) { return; }
                m_AudioSource.Stop();
            }
            m_AudioSource.clip = clip;
            m_AudioSource.loop = looping;
            m_AudioSource.time = 0;
            m_AudioSource.Play();
        }

        void Awake()
        {
            m_AudioSource = GetComponent<AudioSource>();

            if (Instance != null)
            {
                throw new Exception("Multiple ClientMusicPlayer instances!");
            }
            DontDestroyOnLoad(gameObject);
            Instance = this;
            m_AudioSource.volume = ClientPrefs.GetMusicVolume();
        }

        public void SetMusicVolume(float volume)
        {
            m_AudioSource.volume = volume;
        }
    }
}
