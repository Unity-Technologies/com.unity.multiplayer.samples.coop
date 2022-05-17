using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    public class IPHostingUI : MonoBehaviour
    {
        [SerializeField] InputField m_IPInputField;
        [SerializeField] InputField m_PortInputField;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        IPUIMediator m_IPUIMediator;

        [Inject]
        void InjectDependencies(IPUIMediator ipUIMediator)
        {
            m_IPUIMediator = ipUIMediator;
        }

        void Awake()
        {
            m_IPInputField.text = IPUIMediator.k_DefaultIP;
            m_PortInputField.text = IPUIMediator.k_DefaultPort.ToString();
        }

        public void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
        }

        public void OnCreateClick()
        {
            m_IPUIMediator.HostIPRequest(m_IPInputField.text, m_PortInputField.text);
        }

        /// <summary>
        /// Added to the InputField component's OnValueChanged callback for the Room/IP UI text.
        /// </summary>
        public void SanitizeIPInputText()
        {
            m_IPInputField.text = IPUIMediator.Sanitize(m_IPInputField.text);
        }

        /// <summary>
        /// Added to the InputField component's OnValueChanged callback for the Port UI text.
        /// </summary>
        public void SanitizePortText()
        {
            var inputFieldText = IPUIMediator.Sanitize(m_PortInputField.text);
            m_PortInputField.text = inputFieldText;
        }
    }
}
