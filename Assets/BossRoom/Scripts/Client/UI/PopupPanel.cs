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
        CanvasGroup m_CanvasGroup;

        public bool IsDisplaying => m_CanvasGroup.alpha > 0;

        void Awake()
        {
            Hide();
        }

        public void OnConfirmClick()
        {
            Hide();
        }

        public void SetupPopupPanel(string titleText, string mainText)
        {
            m_TitleText.text = titleText;
            m_MainText.text = mainText;
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
