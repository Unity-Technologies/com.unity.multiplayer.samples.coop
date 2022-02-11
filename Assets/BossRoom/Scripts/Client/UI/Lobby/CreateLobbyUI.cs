using System;
using System.Text;
using BossRoom.Scripts.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom;
using UnityEngine;
using UnityEngine.UI;

namespace BossRoom.Scripts.Client.UI
{
    public class CreateLobbyUI : MonoBehaviour
    {
        public const string k_DefaultIP = "127.0.0.1";
        public const int k_DefaultPort = 9998;
        private static readonly char[] k_InputFieldIncludeChars = {'.', '_'};

        [SerializeField] private InputField m_LobbyNameInputField;
        [SerializeField] private CanvasGroup m_IPConnectionCanvasGroup;
        [SerializeField] private InputField m_IPInputField;
        [SerializeField] private InputField m_PortInputField;
        [SerializeField] private GameObject m_LoadingImage;
        [SerializeField] private Toggle m_IPToggle;
        [SerializeField] private Toggle m_UnityRelayToggle;
        [SerializeField] private Toggle m_IsPrivate;
        [SerializeField] private CanvasGroup m_CanvasGroup;

        private LobbyUIMediator m_LobbyUIMediator;

        private OnlineMode m_OnlineMode;

        private void Awake()
        {
            SetOnlineMode(OnlineMode.IpHost);
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
            if (!value) return;

            SetOnlineMode(OnlineMode.IpHost);
        }

        private void UnityRelayRadioRadioButtonPressed(bool value)
        {
            if (!value) return;

            SetOnlineMode(OnlineMode.UnityRelay);
        }


        private void SetOnlineMode(OnlineMode mode)
        {
            m_OnlineMode = mode;

            switch (mode)
            {
                case OnlineMode.IpHost:
                {
                    m_IPConnectionCanvasGroup.alpha = 1;
                    m_IPConnectionCanvasGroup.blocksRaycasts = true;
                    m_IPConnectionCanvasGroup.interactable = true;

                    m_PortInputField.text = k_DefaultPort.ToString();
                    m_PortInputField.text = k_DefaultPort.ToString();
                }
                    break;
                case OnlineMode.UnityRelay:
                {
                    m_IPConnectionCanvasGroup.alpha = 0;
                    m_IPConnectionCanvasGroup.blocksRaycasts = false;
                    m_IPConnectionCanvasGroup.interactable = false;
                }
                    break;
                case OnlineMode.Unset:
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            m_LoadingImage.SetActive(false);
        }

        public void OnCreateClick()
        {
            var portNum = 0;
            int.TryParse(m_PortInputField.text, out portNum);
            if (portNum <= 0)
                portNum = k_DefaultPort;

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
        ///     Sanitize user port InputField box allowing only alphanumerics, plus any matching chars, if provided.
        /// </summary>
        /// <param name="dirtyString"> string to sanitize. </param>
        /// <param name="includeChars"> Array of chars to include. </param>
        /// <returns> Sanitized text string. </returns>
        private static string Sanitize(string dirtyString, char[] includeChars = null)
        {
            var result = new StringBuilder(dirtyString.Length);
            foreach (var c in dirtyString)
                if (char.IsLetterOrDigit(c) ||
                    includeChars != null && Array.Exists(includeChars, includeChar => includeChar == c))
                    result.Append(c);

            return result.ToString();
        }

        /// <summary>
        ///     Added to the InputField component's OnValueChanged callback for the Room/IP UI text.
        /// </summary>
        public void SanitizeIPInputText()
        {
            var inputFieldText = Sanitize(m_IPInputField.text, k_InputFieldIncludeChars);
            m_IPInputField.text = inputFieldText;
        }

        /// <summary>
        ///     Added to the InputField component's OnValueChanged callback for the Port UI text.
        /// </summary>
        public void SanitizePortText()
        {
            var inputFieldText = Sanitize(m_PortInputField.text);
            m_PortInputField.text = inputFieldText;
        }
    }
}
