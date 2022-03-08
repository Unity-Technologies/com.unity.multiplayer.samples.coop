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
        public static void ShowPopupPanel(string titleText, string mainText, bool isCloseableByUser = true)
        {
            if (s_Instance != null)
            {
                s_Instance.SetupPopupPanel(titleText, mainText, isCloseableByUser);
            }
            else
            {
                Debug.LogError($"No PopupPanel instance found. Cannot display message: {titleText}: {mainText}.");
            }
        }

        public static void ClosePopupPanel()
        {
            if (s_Instance != null)
            {
                s_Instance.ResetState();
            }
            else
            {
                Debug.LogError($"No PopupPanel instance found. Cannot close popup.");
            }
        }

        void SetupPopupPanel(string titleText, string mainText, bool isCloseableByUser = false)
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
                m_ConfirmButton.SetActive(isCloseableByUser);
                m_LoadingImage.SetActive(!isCloseableByUser);

                gameObject.SetActive(true);
                m_IsPopupShown = true;
            }
        }
    }
}
