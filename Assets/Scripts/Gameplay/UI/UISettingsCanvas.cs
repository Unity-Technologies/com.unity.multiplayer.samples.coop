using UnityEngine;
using UnityEngine.UIElements;
using Unity.BossRoom.Utils;
using Unity.BossRoom.Audio;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// Controls the special Canvas that has the settings icons and the settings window.
    /// The window itself is controlled by UISettingsPanel; the buttons are controlled here.
    /// </summary>
    public class UISettingsCanvas : MonoBehaviour
    {
        [SerializeField]
        UIDocument uiDocument; // Reference to the UIDocument asset

        VisualElement settingsPanelRoot;
        VisualElement quitPanelRoot;
        Button settingsButton;
        Button quitButton;
        Slider masterVolumeSlider;
        Slider musicVolumeSlider;

        void Awake()
        {
            // Retrieve the root VisualElement from the UIDocument
            var root = uiDocument.rootVisualElement;

            // Query the panels by their names or assigned USS classes
            settingsPanelRoot = root.Q<VisualElement>("settingsPanelRoot");
            quitPanelRoot = root.Q<VisualElement>("quitPanelRoot");
            quitButton = root.Q<Button>("quitButton");
            settingsButton = root.Q<Button>("settingsButton");
            masterVolumeSlider = root.Q<Slider>("masterVolume");
            musicVolumeSlider = root.Q<Slider>("musicVolume");

            // Ensure panels are hidden at startup
            DisablePanels();
            
            settingsButton.SetEnabled(true);
            quitButton.SetEnabled(true);
            // Bind the buttons to their respective method using new input system
            settingsButton.clicked += OnClickSettingsButton;
            quitButton.clicked += OnClickQuitButton;
            
            // Bind the sliders to their respective methods
            masterVolumeSlider.value = ClientPrefs.GetMasterVolume();
            masterVolumeSlider.RegisterValueChangedCallback(evt => OnMasterVolumeSliderChanged(evt.newValue));
            musicVolumeSlider.value = ClientPrefs.GetMusicVolume();
            musicVolumeSlider.RegisterValueChangedCallback(evt => OnMusicVolumeSliderChanged(evt.newValue));
            
            
        }

        void DisablePanels()
        {
            settingsPanelRoot.style.display = DisplayStyle.None;
            quitPanelRoot.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Called directly by the settings button in the UI Through a manual event binding
        /// </summary>
        public void OnClickSettingsButton()
        {
            Debug.Log("Settings button pressed");
            // settingsButton is pressed
            if (settingsPanelRoot != null)
            {
                bool isVisible = settingsPanelRoot.style.display == DisplayStyle.Flex;
                settingsPanelRoot.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (quitPanelRoot != null)
            {
                quitPanelRoot.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Called directly by the quit button in the UI manual button here
        /// </summary>
        public void OnClickQuitButton()
        {
            if (quitPanelRoot != null)
            {
                bool isVisible = quitPanelRoot.style.display == DisplayStyle.Flex;
                quitPanelRoot.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (settingsPanelRoot != null)
            {
                settingsPanelRoot.style.display = DisplayStyle.None;
            }
        }
        
        private void OnMasterVolumeSliderChanged(float newValue)
        {
            ClientPrefs.SetMasterVolume(newValue);
            AudioMixerConfigurator.Instance.Configure();
        }

        private void OnMusicVolumeSliderChanged(float newValue)
        {
            ClientPrefs.SetMusicVolume(newValue);
            AudioMixerConfigurator.Instance.Configure();
        }
    }
}

