using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    public class IPConnectionWindow : MonoBehaviour
    {
        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        IPUIMediator m_IPUIMediator;

        [SerializeField]
        TextMeshProUGUI m_TitleText;

        void Awake()
        {
            Hide();
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
            var chosenTransport = NetworkManager.Singleton.gameObject.GetComponent<TransportPicker>().IpHostTransport;
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = chosenTransport;

            void OnTimeElapsed()
            {
                PopupManager.ShowPopupPanel("Connection Failed",
                    "Failed to connect to server and/or invalid network endpoint");
                Hide();
                m_IPUIMediator.ConnectingFinished();
            }

            switch (chosenTransport)
            {
                case UNetTransport unetTransport:
                    StartCoroutine(WaitUntilUNETDisconnected(OnTimeElapsed));
                    break;
                case UnityTransport unityTransport:
                    var maxConnectAttempts= unityTransport.MaxConnectAttempts;
                    var connectTimeoutMS= unityTransport.ConnectTimeoutMS;
                    StartCoroutine(DisplayUTPReconnectAttempts(maxConnectAttempts, connectTimeoutMS, OnTimeElapsed));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(chosenTransport));
            }

            Show();
        }

        public void Cancel()
        {
            End();
        }

        void End()
        {
            Hide();
            StopAllCoroutines();
        }

        IEnumerator WaitUntilUNETDisconnected(Action endAction)
        {
            yield return new WaitUntil(() => !NetworkManager.Singleton.IsListening);

            endAction();
        }

        IEnumerator DisplayUTPReconnectAttempts(int maxReconnectAttempts, int connectTimeoutMS, Action endAction)
        {
            var attempt = 0;

            while (attempt < maxReconnectAttempts)
            {
                attempt++;
                m_TitleText.text = $"Connecting...{attempt}/{maxReconnectAttempts}.";
                yield return new WaitForSeconds(connectTimeoutMS / 1000f);
            }
            m_TitleText.text = "Connecting...";

            endAction();
        }

        // invoked by UI cancel button
        public void OnCancelJoinButtonPressed()
        {
            m_IPUIMediator.RequestShutdown();
            m_IPUIMediator.ConnectingFinished();
            End();
        }
    }
}
