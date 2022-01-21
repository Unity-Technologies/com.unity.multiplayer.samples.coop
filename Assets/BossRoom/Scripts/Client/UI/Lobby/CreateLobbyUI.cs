using BossRoom.Scripts.Shared.Infrastructure;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.BossRoom;
using UnityEngine;
using UnityEngine.UI;

namespace BossRoom.Scripts.Client.UI
{
    public class CreateLobbyUI : MonoBehaviour
    {

        private LobbyUIManager m_LobbyUIManager;

        private LocalLobby.LobbyData m_lobbyData = new LocalLobby.LobbyData { LobbyName = "New Lobby", MaxPlayerCount = 8, Private = false};

        [SerializeField]
        private InputField m_LobbyNameInputField;

        [SerializeField]
        private InputField m_IPInputField;

        [SerializeField]
        private InputField m_PortInputField;

        OnlineMode m_OnlineMode;

        [SerializeField]
        Toggle m_IPRadioButton;

        [SerializeField]
        Toggle m_RelayRadioButton;

        [SerializeField]
        Toggle m_UnityRelayRadioButton;

        [SerializeField]
        Toggle m_UnityLobbyRadioButton;


        [Inject]
        private void InjectDependencies(LobbyUIManager lobbyUIManager)
        {
            m_LobbyUIManager = lobbyUIManager;
        }

        private void Awake()
        {
        }

        public void SetupEnterGameDisplay(bool enterAsHost, string titleText, string ipHostMainText, string relayMainText, string unityRelayMainText, string inputFieldText,
            string confirmationText, System.Action<string, int, string, OnlineMode> confirmCallback, string defaultIpInput = "", int defaultPortInput = 0)
        {

            m_EnterAsHost = enterAsHost;

            m_DefaultIpInput = defaultIpInput;
            m_DefaultPort = defaultPortInput;

            m_IpHostMainText = ipHostMainText;
            m_RelayMainText = relayMainText;
            m_UnityRelayMainText = unityRelayMainText;

            m_TitleText.text = titleText;
            m_SubText.text = string.Empty;
            m_InputFieldPlaceholderText.text = inputFieldText;
            m_PortInputField.text = defaultPortInput.ToString();
            m_ConfirmFunction = confirmCallback;

            m_ConfirmationText.text = confirmationText;
            m_ConfirmationButton.onClick.AddListener(OnConfirmClick);
            m_ConfirmationButton.gameObject.SetActive(true);

            m_IPRadioButton.gameObject.SetActive(true);
            m_RelayRadioButton.gameObject.SetActive(true);
            m_IPRadioButton.onValueChanged.AddListener(IPRadioRadioButtonPressed);
            m_RelayRadioButton.onValueChanged.AddListener(RelayRadioRadioButtonPressed);
            m_RelayRadioButton.isOn = false;
            m_UnityRelayRadioButton.gameObject.SetActive(true);
            m_UnityRelayRadioButton.onValueChanged.AddListener(UnityRelayRadioRadioButtonPressed);
            m_UnityRelayRadioButton.isOn = false;

            m_UnityLobbyRadioButton.gameObject.SetActive(true);
            m_UnityLobbyRadioButton.onValueChanged.AddListener(UnityLobbyRadioButtonPressed);
            m_UnityLobbyRadioButton.isOn = false;

            m_IPRadioButton.isOn = true;
        }

        private void UnityLobbyRadioButtonPressed(bool value)
        {
            if (!value)
            {
                return;
            }

            if (m_OnlineMode != OnlineMode.Lobby)
            {
                OnlineMode = OnlineMode.Lobby;
            }
        }

        void IPRadioRadioButtonPressed(bool value)
        {
            if (!value)
            {
                return;
            }

            m_OnlineMode = OnlineMode.IpHost;
        }

        void RelayRadioRadioButtonPressed(bool value)
        {
            if (!value)
            {
                return;
            }

            m_OnlineMode = OnlineMode.Relay;
        }

        void UnityRelayRadioRadioButtonPressed(bool value)
        {
            if (!value)
            {
                return;
            }

            m_OnlineMode = OnlineMode.UnityRelay;
        }

        private void OnConfirmClick()
        {
            int portNum = 0;
            int.TryParse(m_PortInputField.text, out portNum);
            if (portNum <= 0)
                portNum = m_DefaultPort;
            m_ConfirmFunction.Invoke(m_InputField.text, portNum, m_NameDisplay.GetCurrentName(), OnlineMode);
        }

        public void OnCreateButtonClicked()
        {



            var lobbyData = new LocalLobby.LobbyData()
            {
                LobbyName = m_LobbyNameInputField.text,

            }

            m_LobbyUIManager.CreateLobbyRequest(m_lobbyData);
        }
    }

}
