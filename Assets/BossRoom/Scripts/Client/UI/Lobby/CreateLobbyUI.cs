using System;
using System.Text;
using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.BossRoom.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace BossRoom.Scripts.Client.UI
{

    public class CreateLobbyUI : MonoBehaviour
    {
        public const string k_DefaultIP = "192.168.1.1";
        public const int k_DefaultPort = 0;

        private LobbyUIMediator m_LobbyUIMediator;

        [SerializeField]
        private InputField m_LobbyNameInputField;

        [SerializeField]
        private InputField m_IPInputField;

        [SerializeField]
        private InputField m_PortInputField;

        private OnlineMode m_OnlineMode;

        #region temp

        [SerializeField]
        [Tooltip("The Animating \"Connecting\" Image you want to animate to show the client is doing something")]
        private GameObject m_LoadingImage;

        [SerializeField]
        private Button m_ConfirmationButton;

        [SerializeField] private Toggle m_IPToggle;
        [SerializeField] private Toggle m_UnityRelayToggle;

        [SerializeField] private Toggle m_IsPrivate;

        private static readonly char[] k_InputFieldIncludeChars = new[] {'.', '_'};

        [SerializeField] private CanvasGroup m_CanvasGroup;


        /// <summary>
        /// Sanitize user port InputField box allowing only alphanumerics, plus any matching chars, if provided.
        /// </summary>
        /// <param name="dirtyString"> string to sanitize. </param>
        /// <param name="includeChars"> Array of chars to include. </param>
        /// <returns> Sanitized text string. </returns>
        private static string Sanitize(string dirtyString, char[] includeChars = null)
        {
            var result = new StringBuilder(dirtyString.Length);
            foreach (char c in dirtyString)
            {
                if (char.IsLetterOrDigit(c) ||
                    (includeChars != null && Array.Exists(includeChars, includeChar => includeChar == c)))
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Added to the InputField component's OnValueChanged callback for the Room/IP UI text.
        /// </summary>
        public void SanitizeIPInputText()
        {
            var inputFieldText = Sanitize(m_IPInputField.text, k_InputFieldIncludeChars);
            m_IPInputField.text = inputFieldText;
        }

        /// <summary>
        /// Added to the InputField component's OnValueChanged callback for the Port UI text.
        /// </summary>
        public void SanitizePortText()
        {
            var inputFieldText = Sanitize(m_PortInputField.text);
            m_PortInputField.text = inputFieldText;
        }

        #endregion

        [Inject]
        private void InjectDependencies(LobbyUIMediator lobbyUIMediator)
        {
            m_LobbyUIMediator = lobbyUIMediator;
        }

        private void Awake()
        {
            SetOnlineMode(OnlineMode.UnityRelay);
            m_IPToggle.onValueChanged.AddListener(IPRadioRadioButtonPressed);
            m_UnityRelayToggle.onValueChanged.AddListener(UnityRelayRadioRadioButtonPressed);
        }

        private void IPRadioRadioButtonPressed(bool value)
        {
            if (!value)
            {
                return;
            }

            SetOnlineMode(OnlineMode.IpHost);
        }

        private void UnityRelayRadioRadioButtonPressed(bool value)
        {
            if (!value)
            {
                return;
            }

            SetOnlineMode(OnlineMode.UnityRelay);
        }


        private void SetOnlineMode(OnlineMode mode)
        {
            m_OnlineMode = mode;

            switch (mode)
            {
                case OnlineMode.IpHost:
                {
                    m_PortInputField.text = k_DefaultPort.ToString();
                    m_IPInputField.gameObject.SetActive(true);

                    m_PortInputField.text = k_DefaultPort.ToString();
                    m_PortInputField.gameObject.SetActive(true);
                }
                    break;
                case OnlineMode.PhotonRelay:
                    throw new NotImplementedException();
                case OnlineMode.UnityRelay:
                {
                    m_IPInputField.gameObject.SetActive(false);
                    m_PortInputField.gameObject.SetActive(false);
                }
                    break;
                case OnlineMode.Unset:
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            m_ConfirmationButton.gameObject.SetActive(true);
            m_LoadingImage.SetActive(false);
        }

        public void OnCreateClick()
        {
            int portNum = 0;
            int.TryParse(m_PortInputField.text, out portNum);
            if (portNum <= 0)
                portNum = k_DefaultPort;

            m_LobbyUIMediator.CreateLobbyRequest( m_LobbyNameInputField.text, m_IsPrivate.isOn, 8, m_OnlineMode, m_IPInputField.text, portNum);
        }

        public void Show()
        {
            m_CanvasGroup.alpha = 1;
            m_CanvasGroup.interactable = true;
            m_CanvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0;
            m_CanvasGroup.interactable = false;
            m_CanvasGroup.blocksRaycasts = false;
        }
    }

}
