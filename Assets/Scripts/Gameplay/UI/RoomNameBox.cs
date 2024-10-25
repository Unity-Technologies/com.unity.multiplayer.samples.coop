using System;
using TMPro;
using Unity.BossRoom.UnityServices.Sessions;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    public class RoomNameBox : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI m_RoomNameText;
        [SerializeField]
        Button m_CopyToClipboardButton;

        LocalSession m_LocalSession;
        string m_SessionCode;

        [Inject]
        private void InjectDependencies(LocalSession localSession)
        {
            m_LocalSession = localSession;
            m_LocalSession.changed += UpdateUI;
        }

        void Awake()
        {
            UpdateUI(m_LocalSession);
        }

        private void OnDestroy()
        {
            m_LocalSession.changed -= UpdateUI;
        }

        private void UpdateUI(LocalSession localSession)
        {
            if (!string.IsNullOrEmpty(localSession.SessionCode))
            {
                m_SessionCode = localSession.SessionCode;
                m_RoomNameText.text = $"Session Code: {m_SessionCode}";
                gameObject.SetActive(true);
                m_CopyToClipboardButton.gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void CopyToClipboard()
        {
            GUIUtility.systemCopyBuffer = m_SessionCode;
        }
    }
}
