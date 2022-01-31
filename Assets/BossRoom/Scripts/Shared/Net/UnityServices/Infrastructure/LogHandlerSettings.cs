using System;
using BossRoom.Scripts.Shared.Infrastructure;
using UnityEngine;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure
{


    /// <summary>
    /// Acts as a buffer between receiving requests to display error messages to the player and running the pop-up UI to do so.
    /// </summary>
    public class LogHandlerSettings : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Only logs of this level or higher will appear in the console.")]
        private LogMode m_editorLogVerbosity = LogMode.Critical;

        // [SerializeField]
        // private PopUpUI m_popUp;

        private IDisposable m_Disposable;

        [Inject]
        private void InjectDependencies( ISubscriber<UnityServiceErrorMessage> errorPopupSubscriber)
        {
            m_Disposable = errorPopupSubscriber.Subscribe(OnReceiveDisplayableErrorMessage);
        }

        private void OnReceiveDisplayableErrorMessage(UnityServiceErrorMessage obj)
        {
            SpawnErrorPopup(obj);
        }

        private void Awake()
        {
            LogHandler.Get().mode = m_editorLogVerbosity;
        }
        private void OnDestroy()
        {
            m_Disposable?.Dispose();
        }

        /// <summary>
        /// For convenience while in the Editor, update the log verbosity when its value is changed in the Inspector.
        /// </summary>
        public void OnValidate()
        {
            LogHandler.Get().mode = m_editorLogVerbosity;
        }

        private void SpawnErrorPopup(UnityServiceErrorMessage error)
        {
            throw new NotImplementedException();
            // m_popUp.ShowPopup(errorMessage.Message);
        }
    }
}
