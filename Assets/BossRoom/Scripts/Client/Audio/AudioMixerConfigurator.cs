using UnityEngine;
using UnityEngine.Audio;

namespace BossRoom.Client
{
    /// <summary>
    /// Initializes the game's AudioMixer to use volumes stored in preferences. Provides
    /// a public function that can be called when these values change.
    /// </summary>
    public class AudioMixerConfigurator : MonoBehaviour
    {
        [SerializeField]
        private AudioMixer m_Mixer;

        [SerializeField]
        private string m_MixerVarMainVolume = "OverallVolume";

        [SerializeField]
        private string m_MixerVarMusicVolume = "MusicVolume";

        [SerializeField]
        private string m_MixerVarSfxVolume = "SfxVolume";

        [SerializeField]
        private string m_MixerVarFootstepsVolume = "FootstepsVolume";

        public static AudioMixerConfigurator Instance { get; private set; }

        /// <summary>
        /// The audio sliders use a value between 0.0001 and 1, but the mixer works in decibels -- by default, -80 to 0.
        /// To convert, we use log10(slider) multiplied by this value. Why 20? because log10(.0001)*20=-80, which is the
        /// bottom range for our mixer, meaning its disabled. 
        /// </summary>
        private const float k_VolumeLog10Multiplier = 20;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // note that trying to configure the AudioMixer during Awake does not work, must be initialized in Start
            Configure();
        }

        public void Configure()
        {
            m_Mixer.SetFloat(m_MixerVarMainVolume, Mathf.Log10(ClientPrefs.GetMasterVolume()) * k_VolumeLog10Multiplier);
            m_Mixer.SetFloat(m_MixerVarMusicVolume, Mathf.Log10(ClientPrefs.GetMusicVolume()) * k_VolumeLog10Multiplier);
        }
    }
}
