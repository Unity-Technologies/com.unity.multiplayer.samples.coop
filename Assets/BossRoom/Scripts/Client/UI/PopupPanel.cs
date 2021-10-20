using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using UnityRegion = Unity.Services.Relay.Models.Region;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// responsible for driving all the functionality of the popup panel players see when connecting to the game
    /// </summary>
    public class PopupPanel : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI m_TitleText;
        [SerializeField]
        private TextMeshProUGUI m_MainText;
        [SerializeField]
        private TextMeshProUGUI m_SubText;
        [SerializeField]
        [Tooltip("The Animating \"Connecting\" Image you want to animate to show the client is doing something")]
        private GameObject m_ReconnectingImage;
        [SerializeField]
        [Tooltip("The GameObject holding the \"Generic\" Input field (for IP address or other address forms)")]
        private InputField m_InputField;
        [SerializeField]
        [Tooltip("The nested Text component of m_InputField, where we stick the placeholder text")]
        private Text m_InputFieldPlaceholderText;
        [SerializeField]
        [Tooltip("The Port-number input field for the panel")]
        private InputField m_PortInputField;
        [SerializeField]
        private Button m_ConfirmationButton;
        [SerializeField]
        private TextMeshProUGUI m_ConfirmationText;
        [SerializeField]
        [Tooltip("This Button appears for popups that ask for player inputs")]
        private Button m_CancelButton;
        [SerializeField]
        private NameDisplay m_NameDisplay;

        OnlineMode m_OnlineMode;

        [SerializeField]
        Toggle m_IPRadioButton;

        [SerializeField]
        Toggle m_RelayRadioButton;

        [SerializeField]
        Toggle m_UnityRelayRadioButton;

        bool m_EnterAsHost;

        string m_IpHostMainText;
        string m_RelayMainText;
        string m_UnityRelayMainText;

        string m_DefaultIpInput;
        int m_DefaultPort;

        /// <summary>
        /// Confirm function invoked when confirm is hit on popup. The meaning of the arguments may vary by popup panel, but
        /// in the initial case of the login popup, they represent the IP Address, port, the Player Name, and the connection mode
        /// </summary>
        private System.Action<string, int, string, OnlineMode> m_ConfirmFunction;

        private const string k_DefaultConfirmText = "OK";

        static readonly char[] k_InputFieldIncludeChars = new[] { '.', '_' };

        /// <summary>
        /// Setup this panel to be a panel view to have the player enter the game, complete with the ability for the player to
        /// cancel their input and requests.
        /// This also adds the player name prompt to the display
        /// </summary>
        /// <param name="enterAsHost">Whether we enter the game as host or client.</param>
        /// <param name="titleText">The Title String at the top of the panel</param>
        /// <param name="ipHostMainText">The text just below the title text. Displays information about how to connect for IpHost mode.</param>
        /// <param name="relayMainText">The text just below the title text. Displays information about how to connect for Relay mode</param>
        /// <param name="unityRelayMainText">The text just below the title text. Displays information about how to connect for Unity Relay mode</param>
        /// <param name="inputFieldText">the text displayed within the input field if empty</param>
        /// <param name="confirmationText"> Text to display on the confirmation button</param>
        /// <param name="confirmCallback">  The delegate to invoke when the player confirms.  It sends what the player input.</param>
        /// <param name="defaultIpInput">The default Ip value to show in the input field.</param>
        /// <param name="defaultPortInput">The default Port# to show in the port-input field.</param>
        public void SetupEnterGameDisplay(bool enterAsHost, string titleText, string ipHostMainText, string relayMainText, string unityRelayMainText, string inputFieldText,
            string confirmationText, System.Action<string, int, string, OnlineMode> confirmCallback, string defaultIpInput = "", int defaultPortInput = 0)
        {
            //Clear any previous settings of the Panel first
            ResetState();

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
            m_IPRadioButton.isOn = true;

            m_CancelButton.onClick.AddListener(OnCancelClick);
            m_CancelButton.gameObject.SetActive(true);

            m_InputField.gameObject.SetActive(true);
            m_PortInputField.gameObject.SetActive(true);

            m_NameDisplay.gameObject.SetActive(true);

            gameObject.SetActive(true);
        }

        void IPRadioRadioButtonPressed(bool value)
        {
            if (!value)
            {
                return;
            }

            m_OnlineMode = OnlineMode.IpHost;
            OnOnlineModeDropdownChanged(m_OnlineMode);
        }

        void RelayRadioRadioButtonPressed(bool value)
        {
            if (!value)
            {
                return;
            }

            m_OnlineMode = OnlineMode.Relay;
            OnOnlineModeDropdownChanged(m_OnlineMode);
        }

        void UnityRelayRadioRadioButtonPressed(bool value)
        {
            if (!value)
            {
                return;
            }

            m_OnlineMode = OnlineMode.UnityRelay;
            OnOnlineModeDropdownChanged(m_OnlineMode);
        }

        private void OnConfirmClick()
        {
            int portNum = 0;
            int.TryParse(m_PortInputField.text, out portNum);
            if (portNum <= 0)
                portNum = m_DefaultPort;
            m_ConfirmFunction.Invoke(m_InputField.text, portNum, m_NameDisplay.GetCurrentName(), m_OnlineMode);
        }

        /// <summary>
        /// Sanitize user port InputField box allowing only alphanumerics, plus any matching chars, if provided.
        /// </summary>
        /// <param name="dirtyString"> string to sanitize. </param>
        /// <param name="includeChars"> Array of chars to include. </param>
        /// <returns> Sanitized text string. </returns>
        static string Sanitize(string dirtyString, char[] includeChars = null)
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
        public void SanitizeInputText()
        {
            var inputFieldText = Sanitize(m_InputField.text, k_InputFieldIncludeChars);
            m_InputField.text = inputFieldText;
        }

        /// <summary>
        /// Added to the InputField component's OnValueChanged callback for the Port UI text.
        /// </summary>
        public void SanitizePortText()
        {
            var inputFieldText = Sanitize(m_PortInputField.text);
            m_PortInputField.text = inputFieldText;
        }

        /// <summary>
        /// Called when the user clicks on the cancel button when in a mode where the player is expecting to input something.
        /// Primary responsibility for this method is to reset the UI state.
        /// </summary>
        private void OnCancelClick()
        {
            ResetState();
        }

        /// <summary>
        /// Called when the user selects a different online mode from the dropdown.
        /// </summary>
        private async void OnOnlineModeDropdownChanged(OnlineMode value)
        {
            // activate this so that it is always activated unless entering as relay host
            m_InputField.gameObject.SetActive(true);

            if (value == OnlineMode.IpHost)
            {
                // Ip host
                m_MainText.text = m_IpHostMainText;
                m_InputField.text = m_DefaultIpInput;
                m_PortInputField.gameObject.SetActive(true);
                m_PortInputField.text = m_DefaultPort.ToString();
            }
            else if (value == OnlineMode.Relay)
            {
                if (string.IsNullOrEmpty(PhotonAppSettings.Instance.AppSettings.AppIdRealtime))
                {
                    if (Application.isEditor)
                    {
                        // If there is no photon app id set tell the user they need to install
                        SetupNotifierDisplay(
                            "Photon Realtime not Setup!", "Follow the instructions in the readme (<ProjectRoot>/Documents/Photon-Realtime/Readme.md) " +
                            "to setup Photon Realtime and use relay mode.", false, true);
                    }
                    else
                    {
                        // If there is no photon app id set tell the user they need to install
                        SetupNotifierDisplay(
                            "Photon Realtime not Setup!", "It needs to be setup in the Unity Editor for this project " +
                            "by following the Photon-Realtime guide, then rebuild the project and distribute it.", false, true);
                    }
                    return;
                }

                // Relay
                m_MainText.text = m_RelayMainText;

                if (m_EnterAsHost)
                {
                    m_InputField.text = GenerateRandomRoomKey();
                    m_InputField.gameObject.SetActive(false);
                }
                else
                {
                    m_InputField.text = "";
                }

                m_PortInputField.gameObject.SetActive(false);
                m_PortInputField.text = "";
            }
            else if (value == OnlineMode.UnityRelay)
            {
                Exception caughtException = null;
                Task<List<UnityRegion>> task = null;
                try
                {
                    await UnityServices.InitializeAsync();
                    if (!AuthenticationService.Instance.IsSignedIn)
                    {
                        await AuthenticationService.Instance.SignInAnonymouslyAsync();
                        var playerId = AuthenticationService.Instance.PlayerId;
                        Debug.Log(playerId);
                    }

                    // calling ListRegion to get a single light health check call. This isn't the best method for this, but is fine to use for now until we
                    // get full QoS endpoints available. MTT-1483
                    task = Relay.Instance.ListRegionsAsync();

                    await task;
                }
                catch (RequestFailedException e)
                {
                    caughtException = e;
                }

                if (task == null || task.IsFaulted || caughtException != null)
                {

                    if (caughtException != null) Debug.LogException(caughtException);
                    if (task != null) Debug.LogException(task.Exception);

                    if (Application.isEditor)
                    {
                        // Error trying to get the list of available regions, something is not setup correctly
                        SetupNotifierDisplay(
                            "Unity Relay error!", "Something went wrong trying to reach Unity Relay. Please follow the instructions here https://docs-multiplayer.unity3d.com/docs/develop/relay/relay/index.html#how-do-I-enable-Relay-for-my-project" +

                                                          "to setup Unity Relay and use relay mode.", false, true);
                    }
                    else
                    {
                        // If there is no photon app id set tell the user they need to install
                        SetupNotifierDisplay(
                            "Unity Relay error!", "Something went wrong trying to reach Unity Relay. It needs to be setup in the Unity Editor for this project " +
                                                       "by following the Unity Relay guide, then rebuild the project and distribute it.", false, true);
                    }

                    return;
                }

                m_MainText.text = m_UnityRelayMainText;
                if (m_EnterAsHost)
                {
                    m_InputField.text = GenerateRandomRoomKey();
                    m_InputField.gameObject.SetActive(false);
                }
                else
                {
                    m_InputField.text = "";
                    m_InputFieldPlaceholderText.text = "Join Code";
                }

                m_PortInputField.gameObject.SetActive(false);
                m_PortInputField.text = "";
            }
        }
        /// <summary>
        /// Generates a random room key to use as a default value.
        /// </summary>
        /// <returns></returns>
        private string GenerateRandomRoomKey()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 6; i++)
            {
                var val = Convert.ToChar(Random.Range(65, 90));
                sb.Append(val);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Helper method to help us reset all state for the popup manager.
        /// </summary>
        private void ResetState()
        {
            m_ConfirmationText.text = k_DefaultConfirmText;
            m_TitleText.text = string.Empty;
            m_MainText.text = string.Empty;
            m_SubText.text = string.Empty;
            m_InputField.text = string.Empty;
            m_PortInputField.text = string.Empty;
            m_ReconnectingImage.SetActive(false);
            m_ConfirmationButton.gameObject.SetActive(false);
            m_CancelButton.gameObject.SetActive(false);
            m_NameDisplay.gameObject.SetActive(false);

            m_IPRadioButton.gameObject.SetActive(false);
            m_RelayRadioButton.gameObject.SetActive(false);
            m_UnityRelayRadioButton.gameObject.SetActive(false);
            m_IPRadioButton.onValueChanged.RemoveListener(IPRadioRadioButtonPressed);
            m_RelayRadioButton.onValueChanged.RemoveListener(RelayRadioRadioButtonPressed);
            m_UnityRelayRadioButton.onValueChanged.RemoveListener(UnityRelayRadioRadioButtonPressed);

            m_CancelButton.onClick.RemoveListener(OnCancelClick);
            m_ConfirmationButton.onClick.RemoveListener(OnConfirmClick);
            m_ConfirmFunction = null;
        }

        /// <summary>
        /// Sets the panel to match the given specifications to notify the player.  If display image is set to true, it will display
        /// </summary>
        /// <param name="titleText">The title text at the top of the panel</param>
        /// <param name="mainText"> The text just under the title- the main body of text</param>
        /// <param name="displayImage">set to true if the notifier should display the animating icon for being busy</param>
        /// <param name="displayConfirmation"> set to true if the panel expects the user to click the button to close the panel.</param>
        /// <param name="subText">optional text in the middle of the panel.  Is not meant to coincide with the displayImage</param>
        public void SetupNotifierDisplay(string titleText, string mainText, bool displayImage, bool displayConfirmation, string subText = "")
        {
            ResetState();

            m_TitleText.text = titleText;
            m_MainText.text = mainText;
            m_SubText.text = subText;

            m_ReconnectingImage.SetActive(displayImage);

            m_ConfirmationButton.gameObject.SetActive(displayConfirmation);
            m_InputField.gameObject.SetActive(false);
            m_PortInputField.gameObject.SetActive(false);
            gameObject.SetActive(true);
        }
    }
}
