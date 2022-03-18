using System;
using UnityEngine;
using TMPro;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// Simple popup panel to display information to players.
    /// </summary>
    public class PopupPanel : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI m_TitleText;
        [SerializeField]
        TextMeshProUGUI m_MainText;

        bool m_IsPopupShown;

        static PopupPanel s_Instance;

        void Awake()
        {
            if (s_Instance != null) throw new Exception("Invalid state, instance is not null");
            s_Instance = this;
            ResetState();
        }

        void OnDestroy()
        {
            s_Instance = null;
        }

        public void OnConfirmClick()
        {
            ResetState();
        }

        /// <summary>
        /// Helper method to help us reset all state for the popup.
        /// </summary>
        void ResetState()
        {
            m_TitleText.text = string.Empty;
            m_MainText.text = string.Empty;
            gameObject.SetActive(false);
            m_IsPopupShown = false;
        }

        /// <summary>
        /// Sets the panel to match the given specifications to notify the player.  If display image is set to true, it will display
        /// </summary>
        /// <param name="titleText">The title text at the top of the panel</param>
        /// <param name="mainText"> The text just under the title- the main body of text</param>
        public static void ShowPopupPanel(string titleText, string mainText)
        {
            if (s_Instance != null)
            {
                s_Instance.SetupPopupPanel(titleText, mainText);
            }
            else
            {
                Debug.LogError($"No PopupPanel instance found. Cannot display message: {titleText}: {mainText}");
            }
        }

        void SetupPopupPanel(string titleText, string mainText)
        {
            if (m_IsPopupShown)
            {
                Debug.LogWarning($"Trying to show popup, but another popup is already being shown. Popup: {titleText}. {mainText}");
            }
            else
            {
                ResetState();

                m_TitleText.text = titleText;
                m_MainText.text = mainText;

                gameObject.SetActive(true);
                m_IsPopupShown = true;
            }
        }
    }
}
