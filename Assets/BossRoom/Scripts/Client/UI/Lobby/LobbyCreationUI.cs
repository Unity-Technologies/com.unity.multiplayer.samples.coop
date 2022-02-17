using System.Text.RegularExpressions;
using BossRoom.Scripts.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom;
using UnityEngine;
using UnityEngine.UI;

namespace BossRoom.Scripts.Client.UI
{
    public class LobbyCreationUI : MonoBehaviour
    {
        public const string k_DefaultIP = "127.0.0.1";
        public const int k_DefaultPort = 9998;

        [SerializeField] private InputField m_LobbyNameInputField;
        [SerializeField] private CanvasGroup m_IPConnectionCanvasGroup;
        [SerializeField] private InputField m_IPInputField;
        [SerializeField] private InputField m_PortInputField;
        [SerializeField] private GameObject m_LoadingIndicatorObject;
        [SerializeField] private Toggle m_IPToggle;
        [SerializeField] private Toggle m_UnityRelayToggle;
        [SerializeField] private Toggle m_IsPrivate;
        [SerializeField] private CanvasGroup m_CanvasGroup;

        private LobbyUIMediator m_LobbyUIMediator;

        private OnlineMode m_OnlineMode;

        private void Awake()
        {
            EnableIPHostUI();
            m_IPToggle.onValueChanged.AddListener(IPRadioRadioButtonPressed);
            m_UnityRelayToggle.onValueChanged.AddListener(UnityRelayRadioRadioButtonPressed);
        }

        [Inject]
        private void InjectDependencies(LobbyUIMediator lobbyUIMediator)
        {
            m_LobbyUIMediator = lobbyUIMediator;
        }

        private void IPRadioRadioButtonPressed(bool value)
        {
            if (!value)
            {
                return;
            }

            EnableIPHostUI();
        }

        private void UnityRelayRadioRadioButtonPressed(bool value)
        {
            if (!value)
            {
                return;
            }

            EnableUnityRelayUI();
        }

        private void EnableUnityRelayUI()
        {
            m_IPConnectionCanvasGroup.alpha = 0;
            m_IPConnectionCanvasGroup.blocksRaycasts = false;
            m_IPConnectionCanvasGroup.interactable = false;

            m_OnlineMode = OnlineMode.UnityRelay;
            
            m_LoadingIndicatorObject.SetActive(false);
        }

        private void EnableIPHostUI()
        {
            m_IPConnectionCanvasGroup.alpha = 1;
            m_IPConnectionCanvasGroup.blocksRaycasts = true;
            m_IPConnectionCanvasGroup.interactable = true;

            m_IPInputField.text = k_DefaultIP;
            m_PortInputField.text = k_DefaultPort.ToString();

            m_OnlineMode = OnlineMode.IpHost;

            m_LoadingIndicatorObject.SetActive(false);
        }

        public void OnCreateClick()
        {
            var portNum = 0;
            int.TryParse(m_PortInputField.text, out portNum);
            if (portNum <= 0)
            {
                portNum = k_DefaultPort;
            }

            var ip = string.IsNullOrEmpty(m_IPInputField.text) ? k_DefaultIP : m_IPInputField.text;

            m_LobbyUIMediator.CreateLobbyRequest(m_LobbyNameInputField.text, m_IsPrivate.isOn, 8, m_OnlineMode, ip, portNum);
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


        /// <summary>
        /// Sanitize user port InputField box allowing only alphanumerics and '.'
        /// </summary>
        /// <param name="dirtyString"> string to sanitize. </param>
        /// <returns> Sanitized text string. </returns>
        private static string Sanitize(string dirtyString)
        {
            return Regex.Replace(dirtyString, "[^A-Za-z0-9.]", "");
        }

        /// <summary>
        /// Added to the InputField component's OnValueChanged callback for the Room/IP UI text.
        /// </summary>
        public void SanitizeIPInputText()
        {
            m_IPInputField.text = Sanitize(m_IPInputField.text);
        }

        /// <summary>
        /// Added to the InputField component's OnValueChanged callback for the Port UI text.
        /// </summary>
        public void SanitizePortText()
        {
            var inputFieldText = Sanitize(m_PortInputField.text);
            m_PortInputField.text = inputFieldText;
        }
    }
}
