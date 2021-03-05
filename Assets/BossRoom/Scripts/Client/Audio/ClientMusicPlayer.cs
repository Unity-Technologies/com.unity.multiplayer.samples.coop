using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace BossRoom.Client
{
    /// <summary>
    /// Music player that handles start of boss battle, victory and restart
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ClientMusicPlayer : MonoBehaviour
    {
        [SerializeField]
        private AudioClip m_ThemeMusic;

        [SerializeField]
        private AudioClip m_BossMusic;

        [SerializeField]
        private AudioClip m_VictoryMusic;

        private AudioSource m_source;


        void Start()
        {
            DontDestroyOnLoad(gameObject);
            m_source = GetComponent<AudioSource>();
        }

        public void RestartTheme()
        {
            PlayTrack(m_ThemeMusic, true, true);
        }

        public void PlayThemeMusic()
        {
            // This can be called while theme is alkready playing - play with restart = false
            PlayTrack(m_ThemeMusic, true, false);
        }

        public void PlayBossMusic()
        {
            // this can be caled multiple times - play with restart = false
            PlayTrack(m_BossMusic, true, false);
        }

        public void PlayVictoryMusic()
        {
            PlayTrack(m_VictoryMusic, false, false);
        }

        private void PlayTrack(AudioClip clip, bool looping, bool restart)
        {
            if (m_source.isPlaying)
            {
                // if we dont want to restart the clip, do nothing if it is playing
                if (!restart && m_source.clip==clip) { return; }
                m_source.Stop();
            }
            m_source.clip = clip;
            m_source.loop = looping;
            m_source.time = 0;
            m_source.Play();
        }
    }
}
