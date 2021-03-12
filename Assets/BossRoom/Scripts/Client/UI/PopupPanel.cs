using System;
using System.Text;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace BossRoom.Visual
{
    /// <summary>
    /// responsible for driving all the functionality of the popup panel players see when connecting to the game
    /// </summary>
    public class PopupPanel : MonoBehaviour
    {
        [SerializeField]
        private Text m_TitleText;
        [SerializeField]
        private Text m_MainText;
        [SerializeField]
        private Text m_SubText;
        [SerializeField]
        [Tooltip("The Animating \"Connecting\" Image you want to animate to show the client is doing something")]
        private GameObject m_ReconnectingImage;
        [SerializeField]
        [Tooltip("The GameObject holding the Input field for the panel")]
        private GameObject m_InputFieldParent;
        [SerializeField]
        private GameObject m_InputBox;
        [SerializeField]
        private Button m_ConfirmationButton;
        [SerializeField]
        private Text m_ConfirmationText;
        [SerializeField]
        [Tooltip("This Button appears for popups that ask for player inputs")]
        private Button m_CancelButton;
        [SerializeField]
        private GameObject m_NameDisplayGO;
        [SerializeField]
        private Dropdown m_OnlineModeDropdown;

        bool m_EnterAsHost;

        string m_IpHostMainText;
        string m_RelayMainText;

        string m_DefaultIpInput;

        /// <summary>
        /// Confirm function invoked when confirm is hit on popup. The meaning of the arguments may vary by popup panel, but
        /// in the initial case of the login popup, they represent the IP Address input, and the Player Name.
        /// </summary>
        private System.Action<string, string, OnlineMode> m_ConfirmFunction;

        private const string k_DefaultConfirmText = "OK";

        /// <summary>
        /// Setup this panel to be a panel view to have the player enter the game, complete with the ability for the player to
        /// cancel their input and requests.
        /// This also adds the player name prompt to the display
        /// </summary>
        /// <param name="enterAsHost">Whether we enter the game as host or client.</param>
        /// <param name="titleText">The Title String at the top of the panel</param>
        /// <param name="ipHostMainText">The text just below the title text. Displays information about how to connect for IpHost mode.</param>
        /// <param name="relayMainText">The text just below the title text. Displays information about how to connect for Relay mode</param>
        /// <param name="inputFieldText">the text displayed within the input field if empty</param>
        /// <param name="confirmationText"> Text to display on the confirmation button</param>
        /// <param name="confirmCallback">  The delegate to invoke when the player confirms.  It sends what the player input.</param>
        /// <param name="defaultIpInput">The default Ip value to show in the input field.</param>
        public void SetupEnterGameDisplay(bool enterAsHost, string titleText, string ipHostMainText, string relayMainText, string inputFieldText,
            string confirmationText, System.Action<string, string, OnlineMode> confirmCallback, string defaultIpInput = "")
        {
            //Clear any previous settings of the Panel first
            ResetState();

            m_EnterAsHost = enterAsHost;

            m_DefaultIpInput = defaultIpInput;

            m_IpHostMainText = ipHostMainText;
            m_RelayMainText = relayMainText;

            m_TitleText.text = titleText;
            m_SubText.text = string.Empty;
            m_InputBox.GetComponent<Text>().text = inputFieldText;
            m_ConfirmFunction = confirmCallback;

            m_ConfirmationText.text = confirmationText;
            m_ConfirmationButton.onClick.AddListener(OnConfirmClick);
            m_ConfirmationButton.gameObject.SetActive(true);

            m_OnlineModeDropdown.gameObject.SetActive(true);
            m_OnlineModeDropdown.value = 0;
            OnOnlineModeDropdownChanged(0);
            m_OnlineModeDropdown.onValueChanged.AddListener(OnOnlineModeDropdownChanged);

            m_CancelButton.onClick.AddListener(OnCancelClick);
            m_CancelButton.gameObject.SetActive(true);

            m_InputFieldParent.SetActive(true);

            m_NameDisplayGO.SetActive(true);

            gameObject.SetActive(true);
        }

        private void OnConfirmClick()
        {
            var inputField = m_InputFieldParent.GetComponent<InputField>();
            var nameDisplay = m_NameDisplayGO.GetComponent<NameDisplay>();
            m_ConfirmFunction.Invoke(inputField.text, nameDisplay.GetCurrentName(), (OnlineMode)m_OnlineModeDropdown.value);
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
        private void OnOnlineModeDropdownChanged(int value)
        {
            var inputField = m_InputFieldParent.GetComponent<InputField>();

            if (value == 0)
            {
                // Ip host
                m_MainText.text = m_IpHostMainText;
                inputField.text = m_DefaultIpInput;
            }
            else
            {
                if (string.IsNullOrEmpty(PhotonAppSettings.Instance.AppSettings.AppIdRealtime))
                {
                    // If there is no photon app id set tell the user they need to install
                    SetupNotifierDisplay("Photon Realtime not Setup!", "Follow the instructions in the readme to setup Photon Realtime and use relay mode.", false, true);
                    return;
                }

                // Relay
                m_MainText.text = m_RelayMainText;

                if (m_EnterAsHost)
                {
                    inputField.text = GenerateRandomRoomKey();
                }
                else
                {
                    inputField.text = "";
                }
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
            var inputField = m_InputFieldParent.GetComponent<InputField>();
            inputField.text = string.Empty;
            m_ReconnectingImage.SetActive(false);
            m_ConfirmationButton.gameObject.SetActive(false);
            m_CancelButton.gameObject.SetActive(false);
            m_NameDisplayGO.gameObject.SetActive(false);

            m_OnlineModeDropdown.gameObject.SetActive(false);
            m_OnlineModeDropdown.onValueChanged.RemoveListener(OnOnlineModeDropdownChanged);

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
            m_InputFieldParent.gameObject.SetActive(false);
            gameObject.SetActive(true);
        }
    }
}
