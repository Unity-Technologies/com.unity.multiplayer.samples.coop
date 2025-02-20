using UnityEngine;
using UnityEngine.UIElements;
using Unity.BossRoom.Utils;
using Unity.BossRoom.Audio;
using Unity.BossRoom.Gameplay.UI; // Reference for UIQuitPanel
using VContainer;                 // Needed for the dependency injection setup

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

        // Panels and Buttons
        VisualElement settingsPanelRoot;
        VisualElement quitPanelRoot;
        Button settingsButton;
        Button quitButton;
        Button qualityButton;
        Button confirmQuitButton;
        Button closeButton;
        Button cancelButton;
        Slider masterVolumeSlider;
        Slider musicVolumeSlider;
        UIQuitPanel uiQuitPanel;

        void Awake()
        {
            // Retrieve the root VisualElement from the UIDocument
            var root = uiDocument.rootVisualElement;
            // get UIQuitPanel component which is attached to the same GameObject
            uiQuitPanel = GetComponentInChildren<UIQuitPanel>();
            // Query the panels by their names or assigned USS classes
            settingsPanelRoot = root.Q<VisualElement>("settingsPanelRoot");
            quitPanelRoot = root.Q<VisualElement>("quitPanelRoot");
            quitButton = root.Q<Button>("quitButton");
            settingsButton = root.Q<Button>("settingsButton");
            qualityButton = root.Q<Button>("qualityButton");
            closeButton = root.Q<Button>("closeButton");
            cancelButton = root.Q<Button>("cancelButton");
            confirmQuitButton = root.Q<Button>("confirmButton");
            masterVolumeSlider = root.Q<Slider>("masterVolume");
            musicVolumeSlider = root.Q<Slider>("musicVolume");

            // Ensure panels are hidden at startup
            DisablePanels();

            // Bind buttons to their respective methods
            settingsButton.clicked += OnClickSettingsButton;
            quitButton.clicked += OnClickQuitButton;
            qualityButton.clicked += SetQualitySettings;
            confirmQuitButton.clicked += ExecuteQuitAction;
            closeButton.clicked += OnClickCloseButton;
            cancelButton.clicked += OnClickCancelButton;

            // Bind sliders to their respective methods
            masterVolumeSlider.value = ClientPrefs.GetMasterVolume();
            masterVolumeSlider.RegisterValueChangedCallback(evt => OnMasterVolumeSliderChanged(evt.newValue));
            musicVolumeSlider.value = ClientPrefs.GetMusicVolume();
            musicVolumeSlider.RegisterValueChangedCallback(evt => OnMusicVolumeSliderChanged(evt.newValue));
        }

        /// <summary>
        /// Ensures all panels are hidden when this component is initialized.
        /// </summary>
        void DisablePanels()
        {
            settingsPanelRoot.style.display = DisplayStyle.None;
            quitPanelRoot.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Called when the Settings button is pressed. Toggles the display of the settings panel.
        /// </summary>
        public void OnClickSettingsButton()
        {
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
        /// Called when the Quit button is pressed. Toggles the display of the quit panel.
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
        
        /// <summary>
        /// Called when the Master Volume slider's value is adjusted.
        /// </summary>
        /// <param name="newValue">New slider value.</param>
        void OnMasterVolumeSliderChanged(float newValue)
        {
           // newValue = Mathf.Clamp(newValue, 0, 100);
            ClientPrefs.SetMasterVolume(newValue);
            AudioMixerConfigurator.Instance.Configure();
            Debug.Log("Master Volume set to: " + newValue);
        }

        /// <summary>
        /// Called when the Music Volume slider's value is adjusted.
        /// </summary>
        /// <param name="newValue">New slider value.</param>
        void OnMusicVolumeSliderChanged(float newValue)
        {
            //float dB= SliderToDecibel(newValue);
            // clamp the value to the range [0, 1]
            //newValue = Mathf.Clamp(newValue, 0, 1);
            ClientPrefs.SetMusicVolume(newValue);
            AudioMixerConfigurator.Instance.Configure();
            Debug.Log("Music Volume set to: " + newValue);
        }

        /// <summary>
        /// Called when the Quality Settings button is pressed. Updates the quality level.
        /// </summary>
        public void SetQualitySettings()
        {
            var qualityLevels = QualitySettings.names.Length - 1;
            var currentLevel = QualitySettings.GetQualityLevel();

            if (currentLevel < qualityLevels)
            {
                QualitySettings.IncreaseLevel();
            }
            else
            {
                QualitySettings.SetQualityLevel(0);
            }

            // Dynamically update the button text with the current quality level
            qualityButton.text = QualitySettings.names[QualitySettings.GetQualityLevel()];
        }

        /// <summary>
        /// Hook for the Quit Panel's Quit functionality. Delegates actions to UIQuitPanel.
        /// </summary>
        void ExecuteQuitAction()
        {
            Debug.Log("Confirm button pressed");
            
            if (uiQuitPanel != null)
            {
                uiQuitPanel.Quit();
                Debug.Log("Quit executed.");
            }
            else
            {
                Debug.LogError("UIQuitPanel is not assigned!");
            }
        }
        
        void OnClickCloseButton()
        {
            // Close the settings panel
            settingsPanelRoot.style.display = DisplayStyle.None;
        }
        
        void OnClickCancelButton()
        {
            // Close the quit panel
            quitPanelRoot.style.display = DisplayStyle.None;
        }
    }
}


