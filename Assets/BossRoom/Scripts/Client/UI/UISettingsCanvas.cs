using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// Controls the special Canvas that has the settings icon and the settings window.
    /// The window itself is controlled by UISettingsPanel; the button is controlled here.
    /// </summary>
    public class UISettingsCanvas : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_SettingsPanelRoot;

        void Awake()
        {
            // the settings canvas should exist in all scenes!
            DontDestroyOnLoad(gameObject);

            // hide the settings window at startup (this is just to handle the common case where an artist forgets to disable the window in the prefab)
            m_SettingsPanelRoot.SetActive(false);
        }

        /// <summary>
        /// Called directly by the settings button in the UI prefab
        /// </summary>
        public void OnClickSettingsButton()
        {
            m_SettingsPanelRoot.SetActive(!m_SettingsPanelRoot.activeSelf);
        }

    }
}
