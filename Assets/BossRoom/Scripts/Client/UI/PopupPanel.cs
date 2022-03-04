using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// Simple popup panel to display information to players.
    /// </summary>
    public class PopupPanel : MonoBehaviour
    {
        struct PopupPanelData
        {
            public string TitleText;
            public string MainText;

            public PopupPanelData(string titleText, string mainText)
            {
                TitleText = titleText;
                MainText = mainText;
            }
        }

        [SerializeField]
        TextMeshProUGUI m_TitleText;
        [SerializeField]
        TextMeshProUGUI m_MainText;

        Stack<PopupPanelData> m_PopupStack = new Stack<PopupPanelData>();

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
            if (m_PopupStack.Count > 0)
            {
                SetupPopupPanel(m_PopupStack.Pop());
            }
        }

        /// <summary>
        /// Helper method to help us reset all state for the popup.
        /// </summary>
        void ResetState()
        {
            m_TitleText.text = string.Empty;
            m_MainText.text = string.Empty;
            gameObject.SetActive(false);
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
                s_Instance.StackPopupPanel(new PopupPanelData(titleText, mainText));
            }
            else
            {
                Debug.LogError($"No PopupPanel instance found. Cannot display message: {titleText}: {mainText}");
            }
        }

        void StackPopupPanel(PopupPanelData data)
        {
            m_PopupStack.Push(data);
            SetupPopupPanel(data);
        }

        void SetupPopupPanel(PopupPanelData data)
        {
            ResetState();

            m_TitleText.text = data.TitleText;
            m_MainText.text = data.MainText;

            gameObject.SetActive(true);
        }
    }
}
