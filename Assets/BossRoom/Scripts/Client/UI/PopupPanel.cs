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
        [SerializeField]
        GameObject m_ConfirmButton;
        [SerializeField]
        GameObject m_LoadingImage;

        bool m_IsPopupShown;

        long m_DisplayedPopupId = -1;
        static long s_NextPopupId = 0;

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
            m_DisplayedPopupId = -1;
            m_TitleText.text = string.Empty;
            m_MainText.text = string.Empty;
            m_ConfirmButton.SetActive(false);
            m_LoadingImage.SetActive(false);
            gameObject.SetActive(false);
            m_IsPopupShown = false;
        }

        /// <summary>
        /// Sets the panel to match the given specifications to notify the player.  If display image is set to true, it will display
        /// </summary>
        /// <param name="titleText">The title text at the top of the panel</param>
        /// <param name="mainText"> The text just under the title- the main body of text</param>
        /// <param name="isCloseableByUser">If true, this popup can be closed by the user, else, it has to be closed manually</param>
        /// <returns>The popup's id</returns>
        public static long ShowPopupPanel(string titleText, string mainText, bool isCloseableByUser = true)
        {
            if (s_Instance == null)
            {
                Debug.LogError($"No PopupPanel instance found. Cannot display message: {titleText}: {mainText}.");
                return -1;
            }

            return s_Instance.SetupPopupPanel(titleText, mainText, isCloseableByUser);
        }

        /// <summary>
        /// Closes the currently displayed popup if it has the requested id.
        /// </summary>
        public static void RequestClosePopupPanel(long popupId)
        {
            if (s_Instance != null)
            {
                s_Instance.ClosePopupPanel(popupId);
            }
            else
            {
                Debug.LogError($"No PopupPanel instance found. Cannot close popup.");
            }
        }

        long SetupPopupPanel(string titleText, string mainText, bool isCloseableByUser = false)
        {
            if (m_IsPopupShown)
            {
                Debug.Log("Trying to show popup, but another popup is already being shown.");
                Debug.Log($"{titleText}. {mainText}");
                return -1;
            }

            ResetState();

            m_TitleText.text = titleText;
            m_MainText.text = mainText;
            m_ConfirmButton.SetActive(isCloseableByUser);
            m_LoadingImage.SetActive(!isCloseableByUser);

            gameObject.SetActive(true);
            m_IsPopupShown = true;
            m_DisplayedPopupId = s_NextPopupId++;
            return m_DisplayedPopupId;
        }

        /// <summary>
        /// Closes the currently displayed popup if it has the requested id.
        /// </summary>
        void ClosePopupPanel(long popupId)
        {
            if (m_DisplayedPopupId == popupId)
            {
                ResetState();
            }
        }
    }
}
