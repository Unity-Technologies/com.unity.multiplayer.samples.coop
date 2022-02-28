using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using Unity.Multiplayer.Samples.BossRoom.Shared.Net.UnityServices.Infrastructure;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// responsible for driving all the functionality of the popup panel players see when connecting to the game
    /// </summary>
    public class PopupPanel : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI m_TitleText;
        [SerializeField]
        TextMeshProUGUI m_MainText;

        /// <summary>
        /// Confirm function invoked when confirm is hit on popup
        /// </summary>
        Action m_ConfirmFunction;

        IDisposable m_Subscriptions;

        static PopupPanel s_Instance;

        void Awake()
        {
            s_Instance = this;
            ResetState();
        }

        void OnDestroy()
        {
            s_Instance = null;
            m_Subscriptions?.Dispose();
        }

        [Inject]
        void InjectDependencies(
            ISubscriber<UnityServiceErrorMessage> unityServiceErrorMessageSub,
            ISubscriber<ConnectStatus> connectStatusSub)
        {
            m_Subscriptions = connectStatusSub.Subscribe(OnConnectStatus);
        }

        void OnConnectStatus(ConnectStatus status)
        {
            switch (status)
            {
                case ConnectStatus.Undefined:
                case ConnectStatus.UserRequestedDisconnect:
                    break;
                case ConnectStatus.ServerFull:
                    SetupNotifierDisplay("Connection Failed", "The Host is full and cannot accept any additional connections.");
                    break;
                case ConnectStatus.Success:
                    break;
                case ConnectStatus.LoggedInAgain:
                    SetupNotifierDisplay("Connection Failed", "You have logged in elsewhere using the same account.");
                    break;
                case ConnectStatus.GenericDisconnect:
                    var title = false ? "Connection Failed" : "Disconnected From Host";
                    var text = false ? "Something went wrong" : "The connection to the host was lost";
                    SetupNotifierDisplay(title, text);
                    break;
                default:
                    Debug.LogWarning($"New ConnectStatus {status} has been added, but no connect message defined for it.");
                    break;
            }
        }

        public void OnConfirmClick()
        {
            m_ConfirmFunction?.Invoke();
            ResetState();
        }

        /// <summary>
        /// Helper method to help us reset all state for the popup manager.
        /// </summary>
        void ResetState()
        {
            m_TitleText.text = string.Empty;
            m_MainText.text = string.Empty;
            m_ConfirmFunction = null;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets the panel to match the given specifications to notify the player.  If display image is set to true, it will display
        /// </summary>
        /// <param name="titleText">The title text at the top of the panel</param>
        /// <param name="mainText"> The text just under the title- the main body of text</param>
        /// <param name="confirmFunction"> The function to call when the confirm button is pressed.</param>
        public static void ShowPopupPanel(string titleText, string mainText, Action confirmFunction = null)
        {
            if (s_Instance != null)
            {
                s_Instance.SetupNotifierDisplay(titleText, mainText, confirmFunction);
            }
            else
            {
                Debug.LogError($"No PopupPanel instance found. Cannot display message: {titleText}: {mainText}");
            }
        }

        void SetupNotifierDisplay(string titleText, string mainText, Action confirmFunction = null)
        {
            ResetState();

            m_TitleText.text = titleText;
            m_MainText.text = mainText;

            m_ConfirmFunction = confirmFunction;
            gameObject.SetActive(true);
        }
    }
}
