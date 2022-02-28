using System;
using UnityEngine;
using UnityEngine.UI;
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
        TextMeshProUGUI m_DisplayText;

        /// <summary>
        /// Confirm function invoked when confirm is hit on popup
        /// </summary>
        Action m_ConfirmFunction;

        static PopupPanel s_Instance;

        void Awake()
        {
            s_Instance = this;
        }

        void OnDestroy()
        {
            s_Instance = null;
        }

        public void OnConfirmClick()
        {
            m_ConfirmFunction.Invoke();
            ResetState();
        }

        /// <summary>
        /// Helper method to help us reset all state for the popup manager.
        /// </summary>
        void ResetState()
        {
            m_TitleText.text = string.Empty;
            m_DisplayText.text = string.Empty;
            m_ConfirmFunction = null;
        }

        /// <summary>
        /// Sets the panel to match the given specifications to notify the player.  If display image is set to true, it will display
        /// </summary>
        /// <param name="titleText">The title text at the top of the panel</param>
        /// <param name="displayText"> The text just under the title- the main body of text</param>
        /// <param name="displayImage">set to true if the notifier should display the animating icon for being busy</param>
        /// <param name="confirmFunction"> The function to call when the confirm button is pressed.</param>
        public static void ShowPopupPanel(string titleText, string displayText, bool displayImage, Action confirmFunction)
        {
            s_Instance.SetupNotifierDisplay(titleText, displayText, displayImage, confirmFunction);
        }

        void SetupNotifierDisplay(string titleText, string mainText, bool displayImage, Action confirmFunction)
        {
            ResetState();

            m_TitleText.text = titleText;
            m_DisplayText.text = mainText;

            m_ConfirmFunction = confirmFunction;
            gameObject.SetActive(true);
        }
    }
}
