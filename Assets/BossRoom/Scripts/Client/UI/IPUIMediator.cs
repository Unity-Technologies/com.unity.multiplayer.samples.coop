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

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField] TextMeshProUGUI m_PlayerNameLabel;

        [SerializeField] IPJoiningUI m_IPJoiningUI;

        [SerializeField] IPHostingUI m_IPHostingUI;

        [SerializeField] UITinter m_JoinTabButtonHighlightTinter;

        [SerializeField] UITinter m_JoinTabButtonTabBlockerTinter;

        [SerializeField] UITinter m_HostTabButtonHighlightTinter;

        [SerializeField] UITinter m_HostTabButtonTabBlockerTinter;

        [SerializeField] GameObject m_SignInSpinner;

        [SerializeField]
        IPConnectionWindow m_IPConnectionWindow;

        NameGenerationData m_NameGenerationData;
        GameNetPortal m_GameNetPortal;
        ClientGameNetPortal m_ClientNetPortal;
        IPublisher<ConnectStatus> m_ConnectStatusPublisher;

        public IPHostingUI IPHostingUI => m_IPHostingUI;

        [Inject]
        void InjectDependenciesAndInitialize(
            NameGenerationData nameGenerationData,
            GameNetPortal gameNetPortal,
            ClientGameNetPortal clientGameNetPortal,
            IPublisher<ConnectStatus> connectStatusPublisher
        )
        {
            m_NameGenerationData = nameGenerationData;
            m_GameNetPortal = gameNetPortal;
            m_ClientNetPortal = clientGameNetPortal;
            m_ConnectStatusPublisher = connectStatusPublisher;

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

        public void HostIPRequest(string ip, string port)
        {
            int.TryParse(port, out var portNum);
            if (portNum <= 0)
            {
                portNum = k_DefaultPort;
            }

            ip = string.IsNullOrEmpty(ip) ? k_DefaultIP : ip;

            m_GameNetPortal.PlayerName = m_PlayerNameLabel.text;

            if (m_GameNetPortal.StartIPServer(ip, portNum, isHost: true))
            {
                m_SignInSpinner.SetActive(true);
            }
            else
            {
                m_ConnectStatusPublisher.Publish(ConnectStatus.StartHostFailed);
            }
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

            m_SignInSpinner.SetActive(true);

            m_ClientNetPortal.StartClient(ip, portNum);

            m_IPConnectionWindow.ShowConnectingWindow();
        }

        public void JoiningWindowCancelled()
        {
            DisableSignInSpinner();
            RequestShutdown();
        }

        public void DisableSignInSpinner()
        {
            m_SignInSpinner.SetActive(false);
        }

        void RequestShutdown()
        {
            if (m_GameNetPortal && m_GameNetPortal.NetManager)
            {
                m_GameNetPortal.NetManager.Shutdown();
            }
        }

        public void RegenerateName()
        {
            m_PlayerNameLabel.text = m_NameGenerationData.GenerateName();
        }

        public void ToggleJoinIPUI()
        {
            m_IPJoiningUI.Show();
            m_IPHostingUI.Hide();
            m_JoinTabButtonHighlightTinter.SetToColor(1);
            m_JoinTabButtonTabBlockerTinter.SetToColor(1);
            m_HostTabButtonHighlightTinter.SetToColor(0);
            m_HostTabButtonTabBlockerTinter.SetToColor(0);
        }

        public void ToggleCreateIPUI()
        {
            m_IPJoiningUI.Hide();
            m_IPHostingUI.Show();
            m_JoinTabButtonHighlightTinter.SetToColor(0);
            m_JoinTabButtonTabBlockerTinter.SetToColor(0);
            m_HostTabButtonHighlightTinter.SetToColor(1);
            m_HostTabButtonTabBlockerTinter.SetToColor(1);
        }

        public void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.interactable = true;
            m_CanvasGroup.blocksRaycasts = true;

            DisableSignInSpinner();
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.interactable = false;
            m_CanvasGroup.blocksRaycasts = false;
        }

        // To be called from the Cancel (X) UI button
        public void CancelConnectingWindow()
        {
            RequestShutdown();
            m_IPConnectionWindow.CancelConnectionWindow();
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
