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
        UIDocument uiDocument;

        // Panels and Buttons
        VisualElement m_SettingsPanelRoot;
        VisualElement m_QuitPanelRoot;
        Button m_SettingsButton;
        Button m_QuitButton;
        Button m_QualityButton;
        Button m_ConfirmQuitButton;
        Button m_CloseButton;
        Button m_CancelButton;
        Slider m_MasterVolumeSlider;
        Slider m_MusicVolumeSlider;
        UIQuitPanel m_UIQuitPanel;

        void Awake()
        {
            var root = uiDocument.rootVisualElement;
            m_UIQuitPanel = GetComponentInChildren<UIQuitPanel>();
            m_SettingsPanelRoot = root.Q<VisualElement>("settingsPanelRoot");
            m_QuitPanelRoot = root.Q<VisualElement>("quitPanelRoot");
            m_QuitButton = root.Q<Button>("quitButton");
            m_SettingsButton = root.Q<Button>("settingsButton");
            m_QualityButton = root.Q<Button>("qualityButton");
            m_CloseButton = root.Q<Button>("closeButton");
            m_CancelButton = root.Q<Button>("cancelButton");
            m_ConfirmQuitButton = root.Q<Button>("confirmButton");
            m_MasterVolumeSlider = root.Q<Slider>("masterVolume");
            m_MusicVolumeSlider = root.Q<Slider>("musicVolume");

            // Ensure panels are hidden at startup
            DisablePanels();

            m_SettingsButton.clicked += OnClickSettingsButton;
            m_QuitButton.clicked += OnClickQuitButton;
            m_QualityButton.clicked += SetQualitySettings;
            m_ConfirmQuitButton.clicked += ExecuteQuitAction;
            m_CloseButton.clicked += OnClickCloseButton;
            m_CancelButton.clicked += OnClickCancelButton;

            // Bind sliders to their respective methods
            m_MasterVolumeSlider.value = ClientPrefs.GetMasterVolume();
            m_MasterVolumeSlider.RegisterValueChangedCallback(evt => OnMasterVolumeSliderChanged(evt.newValue));
            m_MusicVolumeSlider.value = ClientPrefs.GetMusicVolume();
            m_MusicVolumeSlider.RegisterValueChangedCallback(evt => OnMusicVolumeSliderChanged(evt.newValue));
        }

        /// <summary>
        /// Ensures all panels are hidden when this component is initialized.
        /// </summary>
        void DisablePanels()
        {
            m_SettingsPanelRoot.style.display = DisplayStyle.None;
            m_QuitPanelRoot.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Called when the Settings button is pressed. Toggles the display of the settings panel.
        /// </summary>
        public void OnClickSettingsButton()
        {
            if (m_SettingsPanelRoot != null)
            {
                bool isVisible = m_SettingsPanelRoot.style.display == DisplayStyle.Flex;
                m_SettingsPanelRoot.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (m_QuitPanelRoot != null)
            {
                m_QuitPanelRoot.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Called when the Quit button is pressed. Toggles the display of the quit panel.
        /// </summary>
        public void OnClickQuitButton()
        {
            if (m_QuitPanelRoot != null)
            {
                bool isVisible = m_QuitPanelRoot.style.display == DisplayStyle.Flex;
                m_QuitPanelRoot.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (m_SettingsPanelRoot != null)
            {
                m_SettingsPanelRoot.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Called when the Master Volume slider's value is adjusted.
        /// </summary>
        /// <param name="newValue">New slider value.</param>
        void OnMasterVolumeSliderChanged(float newValue)
        {
            ClientPrefs.SetMasterVolume(newValue);
            AudioMixerConfigurator.Instance.Configure();
        }

        /// <summary>
        /// Called when the Music Volume slider's value is adjusted.
        /// </summary>
        /// <param name="newValue">New slider value.</param>
        void OnMusicVolumeSliderChanged(float newValue)
        {
            ClientPrefs.SetMusicVolume(newValue);
            AudioMixerConfigurator.Instance.Configure();
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
            m_QualityButton.text = QualitySettings.names[QualitySettings.GetQualityLevel()];
        }

        /// <summary>
        /// Hook for the Quit Panel's Quit functionality. Delegates actions to UIQuitPanel.
        /// </summary>
        void ExecuteQuitAction()
        {
            if (m_UIQuitPanel != null)
            {
                m_UIQuitPanel.Quit();
            }
            else
            {
                Debug.LogError("UIQuitPanel is not assigned!");
            }
        }

        void OnClickCloseButton()
        {
            // Close the settings panel
            m_SettingsPanelRoot.style.display = DisplayStyle.None;
        }

        void OnClickCancelButton()
        {
            // Close the quit panel
            m_QuitPanelRoot.style.display = DisplayStyle.None;
        }
    }
}
