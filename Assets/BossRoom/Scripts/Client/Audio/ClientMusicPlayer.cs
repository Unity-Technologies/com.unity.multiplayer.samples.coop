using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
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

        [SerializeField]
        private AudioSource m_source;

        /// <summary>
        /// static accessor for ClientMusicPlayer
        /// </summary>
        public static ClientMusicPlayer Instance { get; private set; }

        void Start()
        {

        }

        public void PlayThemeMusic(bool restart)
        {
            PlayTrack(m_ThemeMusic, true, restart);
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

        private void Awake()
        {
            m_source = GetComponent<AudioSource>();

            if (Instance != null)
            {
                throw new System.Exception("Multiple ClientMuscPlayers!");
            }
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
    }
}
