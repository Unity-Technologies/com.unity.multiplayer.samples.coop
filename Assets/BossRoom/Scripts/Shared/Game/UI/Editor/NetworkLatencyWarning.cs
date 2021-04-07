using System;
using UnityEngine;
using UnityEngine.UI;
using MLAPI;
using MLAPI.Transports;
using MLAPI.Transports.LiteNetLib;
using MLAPI.Transports.PhotonRealtime;
using UnityEngine.Assertions;

namespace BossRoom.Scripts.Editor
{
    public class NetworkLatencyWarning : MonoBehaviour
    {
        Text m_LatencyText;
        bool m_LatencyTextCreated;

        Color m_TextColor = Color.red;

        bool m_ArtificialLatencyEnabled;

        void Update()
        {
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            {
                var chosenTransport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;

                switch (chosenTransport)
                {
                    // adding this preprocessor directive check since LiteNetLib only injects latency in #DEBUG
                    #if DEBUG
                    case LiteNetLibTransport liteNetLibTransport:
                        m_ArtificialLatencyEnabled = liteNetLibTransport.SimulatePacketLossChance > 0 ||
                            liteNetLibTransport.SimulateMinLatency > 0 ||
                            liteNetLibTransport.SimulateMaxLatency > 0;
                        break;
                    #endif
                    case MLAPI.Transports.UNET.UNetTransport unetTransport:
                    case PhotonRealtimeTransport photonTransport:
                        m_ArtificialLatencyEnabled = false;
                        break;
                    default:
                        throw new Exception($"unhandled transport {chosenTransport.GetType()}");
                }

                if (m_ArtificialLatencyEnabled)
                {
                    if (!m_LatencyTextCreated)
                    {
                        m_LatencyTextCreated = true;
                        CreateLatencyText();
                    }

                    m_TextColor.a = Mathf.PingPong(Time.time, 1f);
                    m_LatencyText.color = m_TextColor;
                }
            }
        }

        // Creating a UI text object and add it to NetworkOverlay canvas
        void CreateLatencyText()
        {
            Assert.IsNotNull(Scripts.Editor.NetworkOverlay.Instance,
                "No NetworkOverlay object part of scene. Add NetworkOverlay prefab to bootstrap scene!");

            var statUI = new GameObject("UI Latency Warning Text");

            m_LatencyText = statUI.AddComponent<Text>();
            m_LatencyText.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            m_LatencyText.horizontalOverflow = HorizontalWrapMode.Overflow;
            m_LatencyText.alignment = TextAnchor.MiddleLeft;
            m_LatencyText.raycastTarget = false;
            m_LatencyText.resizeTextForBestFit = true;

            m_LatencyText.text = "Network Latency Enabled";

            var statUIRectTransform = statUI.GetComponent<RectTransform>();
            NetworkOverlay.Instance.AddToUI(statUIRectTransform);
        }
    }
}
