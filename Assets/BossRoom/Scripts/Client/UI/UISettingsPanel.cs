using UnityEngine;
using UnityEngine.UI;

namespace BossRoom.Client
{

    public class UISettingsPanel : MonoBehaviour
    {
        [SerializeField]
        private Slider m_MasterVolumeSlider;

        [SerializeField]
        private Slider m_MusicVolumeSlider;

        private void OnEnable()
        {
            // Note that we initialize the slider BEFORE we listen for changes (so we don't get notified of our own change!)
            m_MasterVolumeSlider.value = ClientPrefs.GetMasterVolume();
            m_MasterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeSliderChanged);

            // initialize music slider similarly.
            m_MusicVolumeSlider.value = ClientPrefs.GetMusicVolume();
            m_MusicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeSliderChanged);
        }

        private void OnDisable()
        {
            m_MasterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeSliderChanged);
            m_MusicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeSliderChanged);
        }

        private void OnMasterVolumeSliderChanged(float newValue)
        {
            AudioListener.volume = newValue;
            ClientPrefs.SetMasterVolume(newValue);
        }

        private void OnMusicVolumeSliderChanged(float newValue)
        {
            ClientMusicPlayer.Instance.SetMusicVolume(newValue);
            ClientPrefs.SetMusicVolume(newValue);
        }
    }

}
