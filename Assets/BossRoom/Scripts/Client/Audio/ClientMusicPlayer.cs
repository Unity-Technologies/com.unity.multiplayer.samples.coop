using UnityEngine;

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

        [SerializeField]
        AudioSource m_source;

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

        void PlayTrack(AudioClip clip, bool looping, bool restart)
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

        void Awake()
        {
            m_source = GetComponent<AudioSource>();

            if (Instance != null)
            {
                throw new Exception("Multiple ClientMuscPlayers!");
            }
                        m_source = GetComponent<AudioSource>();
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
    }
}
