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
        [SerializeField]
        CanvasGroup m_CanvasGroup;

        Stack<PopupPanelData> m_PopupStack = new Stack<PopupPanelData>();

        static PopupPanel s_Instance;

        void Awake()
        {
            if (s_Instance != null) throw new Exception("Invalid state, instance is not null");
            s_Instance = this;
            Hide();
        }

        void OnDestroy()
        {
            s_Instance = null;
        }

        public void OnConfirmClick()
        {
            m_PopupStack.Pop();
            if (m_PopupStack.Count > 0)
            {
                SetupPopupPanel(m_PopupStack.Peek());
            }
            else
            {
                Hide();
            }
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
            m_TitleText.text = data.TitleText;
            m_MainText.text = data.MainText;

            Show();
        }



        void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
        }

        void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
        }
    }
}
