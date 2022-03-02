using System;
using UnityEngine;
using TMPro;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// responsible for driving all the functionality of the popup panel players see when connecting to the game
    /// </summary>
    public class PopupPanel : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI m_TitleText;
        [SerializeField]
        TextMeshProUGUI m_MainText;

        bool m_IsPopupShown;

        void Awake()
        {
            ResetState();
        }

        public void OnConfirmClick()
        {
            ResetState();
        }

        /// <summary>
        /// Helper method to help us reset all state for the popup manager.
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
        public void ShowPopupPanel(string titleText, string mainText)
        {
            SetupPopupPanel(titleText, mainText);
        }

        void SetupPopupPanel(string titleText, string mainText)
        {
            if (m_IsPopupShown)
            {
                Debug.Log("Trying to show popup, but another popup is already being shown.");
                Debug.Log($"{titleText}. {mainText}");
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
