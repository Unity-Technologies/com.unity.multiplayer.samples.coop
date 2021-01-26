using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// responsible for driving all the functionality of the popup panel players see when connecting to the game
/// </summary>
public class PopupPanelManager : MonoBehaviour
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


    private const string k_DEFAULT_CONFIRM_TEXT = "OK";


    public delegate void OnConfirmFunction(string input);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="titleText"></param>
    /// <param name="inputFieldText"></param>
    /// <param name="confirmationText"></param>
    /// <param name="confirmCallback"></param>
    public void SetupInputDisplay(string titleText, string mainText, string inputFieldText, string confirmationText, OnConfirmFunction confirmCallback)
    {
        m_TitleText.text = titleText;
        m_MainText.text = mainText;
        m_SubText.text = "";
        m_InputBox.GetComponent<Text>().text = inputFieldText;
        m_ConfirmFunction = confirmCallback;

   
        m_ConfirmationButton.GetComponentInChildren<Text>().text = confirmationText;
        m_ConfirmationButton.onClick.AddListener(OnConfirmClick);
        m_ConfirmationButton.gameObject.SetActive(true);

        m_CancelButton.onClick.AddListener(onCancelClick);
        m_CancelButton.gameObject.SetActive(true);

        gameObject.SetActive(true);
    }


    private void OnConfirmClick()
    {
        var inputField = m_InputFieldParent.GetComponent<InputField>();
        m_ConfirmFunction.Invoke(inputField.text);

        ResetState();
    }

    /// <summary>
    /// Called when the user clicks on the cancel button when in a mode where the player is expecting to input something.
    /// Primary responsibility for this method is to reset the UI state.
    /// </summary>
    private void onCancelClick()
    {
        ResetState();
    }

    /// <summary>
    /// Helper method to help us reset all state for the popup manager. 
    /// </summary>
    private void ResetState()
    {
        m_ConfirmationButton.GetComponentInChildren<Text>().text = k_DEFAULT_CONFIRM_TEXT;
        m_TitleText.text = "";
        m_MainText.text = "";
        m_SubText.text = "";
        m_ReconnectingImage.SetActive(false);
        m_ConfirmationButton.gameObject.SetActive(false);
        m_CancelButton.gameObject.SetActive(false);

        m_CancelButton.onClick.RemoveListener(onCancelClick);
        m_ConfirmationButton.onClick.RemoveListener(OnConfirmClick);
        m_ConfirmFunction = null;
    }

    /// <summary>
    /// Sets the notifier panel to match the given specificiations.  
    /// </summary>
    /// <param name="titleText">The title text at the top of the panel</param>
    /// <param name="displayImage">set to true if the notifier should display the animating icon for being busy</param>
    /// <param name="displayConfirmation"> set to true if the panel expects the user to click the button to close the panel.</param>
    /// <param name="subText">optional text in the middle of the panel.  Is not meant to coincide with the displayImage</param>
    public void SetNotifierAndDisplay(string titleText, bool displayImage, bool displayConfirmation, string subText = "")
    {
        m_TitleText.text = titleText;
        m_SubText.text = subText;

        if (displayImage)
        {
            m_ReconnectingImage.SetActive(true);
            //TODO: Fix for Animating image
        }
        else
        {
            m_ReconnectingImage.SetActive(true);
        }

        m_ConfirmationButton.gameObject.SetActive(displayConfirmation);
        m_InputFieldParent.gameObject.SetActive(false);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
