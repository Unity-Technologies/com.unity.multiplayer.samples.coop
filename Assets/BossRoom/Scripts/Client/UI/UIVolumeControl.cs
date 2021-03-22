using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls a master-volume Slider.
/// </summary>
public class UIVolumeControl : MonoBehaviour
{
    [SerializeField]
    private Slider m_VolumeSlider;

    private void OnEnable()
    {
        m_VolumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
    }

    private void OnDisable()
    {
        m_VolumeSlider.onValueChanged.RemoveListener(OnVolumeSliderChanged);
    }

    private void Update()
    {
        // we want to make sure the slider is 100% accurate at all times, even if it gets changed
        // from some other UI screen rather than our slider. (Most games have a "settings" window where things
        // like master volume can also be configured.) So we constantly check to see if the volume has changed.
        // (This also initializes the slider to the correct value at startup... otherwise we would do this in OnEnable())
        if (AudioListener.volume != m_VolumeSlider.value)
        {
            m_VolumeSlider.value = AudioListener.volume;
        }
    }

    private void OnVolumeSliderChanged(float newValue)
    {
        AudioListener.volume = newValue;
        ClientPrefs.SetMasterVolume(newValue);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // default assumption is that if there's a Slider on our GameObject, we probably want to use it!
        if (m_VolumeSlider == null)
            m_VolumeSlider = GetComponent<Slider>();
    }
#endif
}
