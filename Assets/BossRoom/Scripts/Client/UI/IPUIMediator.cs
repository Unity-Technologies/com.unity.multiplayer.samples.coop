using System;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom.Client;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    public class IPUIMediator : MonoBehaviour
    {
        public const string k_DefaultIP = "127.0.0.1";
        public const int k_DefaultPort = 9998;

        const int k_NbTouchesToOpenWindow = 4;
        const KeyCode m_OpenIPWindowKeyCode = KeyCode.Slash;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField] TextMeshProUGUI m_PlayerNameLabel;

        [SerializeField] IPJoiningUI m_IPJoiningUI;

        [SerializeField] IPHostingUI m_IPHostingUI;

        [SerializeField] UITinter m_JoinTabButtonTinter;

        [SerializeField] UITinter m_JoinTabButtonBorderTinter;

        [SerializeField] UITinter m_HostTabButtonTinter;

        [SerializeField] UITinter m_HostTabButtonBorderTinter;

        [SerializeField] GameObject m_SignInSpinner;

        NameGenerationData m_NameGenerationData;
        GameNetPortal m_GameNetPortal;
        ClientGameNetPortal m_ClientNetPortal;

        [Inject]
        void InjectDependenciesAndInitialize(
            NameGenerationData nameGenerationData,
            GameNetPortal gameNetPortal,
            ClientGameNetPortal clientGameNetPortal
        )
        {
            m_NameGenerationData = nameGenerationData;
            m_GameNetPortal = gameNetPortal;
            m_ClientNetPortal = clientGameNetPortal;

            RegenerateName();
        }

        void Awake()
        {
            Hide();
        }

        void Start()
        {
            // show create IP as default
            ToggleCreateIPUI();
        }

        static bool AnyTouchDown()
        {
            foreach (var touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    return true;
                }
            }
            return false;
        }

        public void HostIPRequest(string ip, string port)
        {
            int.TryParse(port, out var portNum);
            if (portNum <= 0)
            {
                portNum = k_DefaultPort;
            }

            ip = string.IsNullOrEmpty(ip) ? k_DefaultIP : ip;

            m_GameNetPortal.PlayerName = m_PlayerNameLabel.text;

            m_GameNetPortal.StartHost(ip, portNum);

            m_SignInSpinner.SetActive(true);
        }

        public void JoinWithIP(string ip, string port)
        {
            int.TryParse(port, out var portNum);
            if (portNum <= 0)
            {
                portNum = k_DefaultPort;
            }

            ip = string.IsNullOrEmpty(ip) ? k_DefaultIP : ip;

            m_GameNetPortal.PlayerName = m_PlayerNameLabel.text;

            m_ClientNetPortal.StartClient(ip, portNum);

            m_SignInSpinner.SetActive(true);
        }

        public void RegenerateName()
        {
            m_PlayerNameLabel.text = m_NameGenerationData.GenerateName();
        }

        public void ToggleJoinIPUI()
        {
            m_IPJoiningUI.Show();
            m_IPHostingUI.Hide();
            m_JoinTabButtonTinter.SetToColor(1);
            m_JoinTabButtonBorderTinter.SetToColor(1);
            m_HostTabButtonTinter.SetToColor(0);
            m_HostTabButtonBorderTinter.SetToColor(0);
        }

        public void ToggleCreateIPUI()
        {
            m_IPJoiningUI.Hide();
            m_IPHostingUI.Show();
            m_JoinTabButtonTinter.SetToColor(0);
            m_JoinTabButtonBorderTinter.SetToColor(0);
            m_HostTabButtonTinter.SetToColor(1);
            m_HostTabButtonBorderTinter.SetToColor(1);
        }

        public void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.interactable = true;
            m_CanvasGroup.blocksRaycasts = true;

            m_SignInSpinner.SetActive(false);
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.interactable = false;
            m_CanvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Sanitize user port InputField box allowing only alphanumerics and '.'
        /// </summary>
        /// <param name="dirtyString"> string to sanitize. </param>
        /// <returns> Sanitized text string. </returns>
        public static string Sanitize(string dirtyString)
        {
            return Regex.Replace(dirtyString, "[^A-Za-z0-9.]", "");
        }
    }
}
