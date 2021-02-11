using UnityEngine;
using UnityEngine.UI;

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
        [Tooltip("This Button appears for popups that ask for player inputs")]
        private Button m_CancelButton;

        private OnConfirmFunction m_ConfirmFunction;


        private const string k_DefaultConfirmText = "OK";


        public delegate void OnConfirmFunction(string input);

        /// <summary>
        /// Setup this panel to be an input accepting panel, complete with the ability for the player to cancel their input 
        /// </summary>
        /// <param name="titleText">The Title String at the top of the panel</param>
        /// <param name="mainText"> The text just below the title text</param>
        /// <param name="inputFieldText">the text displayed within the input field if empty</param>
        /// <param name="confirmationText"> Text to display on the confirmation button</param>
        /// <param name="confirmCallback">  The delegate to invoke when the player confirms.  It sends what the player input.</param>
        /// <param name="defaultInput"> If Set, will default the input value to this string</param>
        public void SetupInputDisplay(string titleText, string mainText, string inputFieldText,
            string confirmationText, OnConfirmFunction confirmCallback, string defaultInput = "")
        {
            //Clear any previous settings of the Panel first
            ResetState();

            m_TitleText.text = titleText;
            m_MainText.text = mainText;
            m_SubText.text = string.Empty;
            m_InputBox.GetComponent<Text>().text = inputFieldText;
            m_ConfirmFunction = confirmCallback;


            m_ConfirmationButton.GetComponentInChildren<Text>().text = confirmationText;
            m_ConfirmationButton.onClick.AddListener(OnConfirmClick);
            m_ConfirmationButton.gameObject.SetActive(true);

            m_CancelButton.onClick.AddListener(OnCancelClick);
            m_CancelButton.gameObject.SetActive(true);

            m_InputFieldParent.SetActive(true);
            var inputField = m_InputFieldParent.GetComponent<InputField>();
            inputField.text = defaultInput;

            gameObject.SetActive(true);
        }


        private void OnConfirmClick()
        {
            var inputField = m_InputFieldParent.GetComponent<InputField>();
            m_ConfirmFunction.Invoke(inputField.text);

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
        /// Helper method to help us reset all state for the popup manager. 
        /// </summary>
        private void ResetState()
        {
            m_ConfirmationButton.GetComponentInChildren<Text>().text = k_DefaultConfirmText;
            m_TitleText.text = string.Empty;
            m_MainText.text = string.Empty;
            m_SubText.text = string.Empty;
            var inputField = m_InputFieldParent.GetComponent<InputField>();
            inputField.text = string.Empty;
            m_ReconnectingImage.SetActive(false);
            m_ConfirmationButton.gameObject.SetActive(false);
            m_CancelButton.gameObject.SetActive(false);


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

