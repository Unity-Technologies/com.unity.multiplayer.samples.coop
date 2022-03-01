using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure;
using UnityEngine.SceneManagement;

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

        /// <summary>
        /// Confirm function invoked when confirm is hit on popup
        /// </summary>
        Action m_ConfirmFunction;

        static PopupPanel s_Instance;

        void Awake()
        {
            s_Instance = this;
            ResetState();
        }

        void OnDestroy()
        {
            s_Instance = null;
        }

        public void OnConfirmClick()
        {
            m_ConfirmFunction?.Invoke();
            ResetState();
        }

        /// <summary>
        /// Helper method to help us reset all state for the popup manager.
        /// </summary>
        void ResetState()
        {
            m_TitleText.text = string.Empty;
            m_MainText.text = string.Empty;
            m_ConfirmFunction = null;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets the panel to match the given specifications to notify the player.  If display image is set to true, it will display
        /// </summary>
        /// <param name="titleText">The title text at the top of the panel</param>
        /// <param name="mainText"> The text just under the title- the main body of text</param>
        /// <param name="confirmFunction"> The function to call when the confirm button is pressed.</param>
        public static void ShowPopupPanel(string titleText, string mainText, Action confirmFunction = null)
        {
            if (s_Instance != null)
            {
                s_Instance.SetupPopupPanel(titleText, mainText, confirmFunction);
            }
            else
            {
                Debug.LogError($"No PopupPanel instance found. Cannot display message: {titleText}: {mainText}");
            }
        }

        void SetupPopupPanel(string titleText, string mainText, Action confirmFunction = null)
        {
            ResetState();

            m_TitleText.text = titleText;
            m_MainText.text = mainText;

            m_ConfirmFunction = confirmFunction;
            gameObject.SetActive(true);
        }
    }
}
