using System;
using System.Collections;
using TMPro;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Infrastructure;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    public class IPConnectionWindow : MonoBehaviour
    {
        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        TextMeshProUGUI m_TitleText;

        [Inject] IPUIMediator m_IPUIMediator;

        ISubscriber<ConnectStatus> m_ConnectStatusSubscriber;

        [Inject]
        void InjectDependencies(ISubscriber<ConnectStatus> connectStatusSubscriber)
        {
            m_ConnectStatusSubscriber = connectStatusSubscriber;
            m_ConnectStatusSubscriber.Subscribe(OnConnectStatusMessage);
        }

        void Awake()
        {
            Hide();
        }

        void OnDestroy()
        {
            if (m_ConnectStatusSubscriber != null)
            {
                m_ConnectStatusSubscriber.Unsubscribe(OnConnectStatusMessage);
            }
        }

        void OnConnectStatusMessage(ConnectStatus connectStatus)
        {
            CancelConnectionWindow();
            m_IPUIMediator.DisableSignInSpinner();
        }

        void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = true;
        }

        void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.blocksRaycasts = false;
        }

        public void ShowConnectingWindow()
        {
            void OnTimeElapsed()
            {
                Hide();
                m_IPUIMediator.DisableSignInSpinner();
            }

            var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            var maxConnectAttempts = utp.MaxConnectAttempts;
            var connectTimeoutMS = utp.ConnectTimeoutMS;
            StartCoroutine(DisplayUTPConnectionDuration(maxConnectAttempts, connectTimeoutMS, OnTimeElapsed));

            Show();
        }

        public void CancelConnectionWindow()
        {
            Hide();
            StopAllCoroutines();
        }

        IEnumerator DisplayUTPConnectionDuration(int maxReconnectAttempts, int connectTimeoutMS, Action endAction)
        {
            var connectionDuration = maxReconnectAttempts * connectTimeoutMS / 1000f;

            var seconds = Mathf.CeilToInt(connectionDuration);

            while (seconds > 0)
            {
                m_TitleText.text = $"Connecting...\n{seconds}";
                yield return new WaitForSeconds(1f);
                seconds--;
            }
            m_TitleText.text = "Connecting...";

            endAction();
        }

        // invoked by UI cancel button
        public void OnCancelJoinButtonPressed()
        {
            CancelConnectionWindow();
            m_IPUIMediator.JoiningWindowCancelled();
        }
    }
}
