using System;
using UnityEngine;
using MLAPI;

namespace BossRoom.Scripts.Editor
{
    public class NetworkOverlay : MonoBehaviour
    {
        bool m_ArtificialLatencyEnabled;
        Color m_TextColor = Color.red;
        GUIStyle m_TextStyle = GUIStyle.none;
        GUIStyle m_NetworkStatsStyle = GUIStyle.none;
        Texture2D m_BackgroundTexture;

        int height;

        void Awake()
        {
            DontDestroyOnLoad(this);

            m_BackgroundTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            m_BackgroundTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.7f));
            m_BackgroundTexture.Apply();

            m_TextStyle.fontSize = 24;
            m_TextStyle.normal.background = m_BackgroundTexture;

            m_NetworkStatsStyle.fontSize = 24;
        }

        #if DEBUG
        void OnGUI()
        {
            height = Screen.height - 100;
            GUILayout.BeginArea(new Rect(50, height, 280, 400));
            if (NetworkingManager.Singleton.IsClient || NetworkingManager.Singleton.IsServer)
            {
                var chosenTransport = NetworkingManager.Singleton.NetworkConfig.NetworkTransport;

                switch (chosenTransport)
                {
                    case LiteNetLibTransport.LiteNetLibTransport liteNetLibTransport:
                        m_ArtificialLatencyEnabled = liteNetLibTransport.SimulatePacketLossChance > 0 ||
                            liteNetLibTransport.SimulateMinLatency > 0 ||
                            liteNetLibTransport.SimulateMaxLatency > 0;
                        break;
                    case MLAPI.Transports.UNET.UnetTransport unetTransport:
                        m_ArtificialLatencyEnabled = false;
                        break;
                    default:
                        throw new Exception($"unhandled transport {chosenTransport.GetType()}");
                }

                m_TextStyle.normal.textColor = Color.white;
                if (!string.IsNullOrEmpty(NetworkStats.Text))
                {
                    GUILayout.Label(NetworkStats.Text, m_TextStyle);
                }

                if (m_ArtificialLatencyEnabled)
                {
                    m_TextColor.a = Mathf.PingPong(Time.time, 1f);
                    m_TextStyle.normal.textColor = m_TextColor;

                    GUILayout.Label("Artificial Latency Enabled", m_TextStyle);
                }
            }

            GUILayout.EndArea();
        }
        #endif
    }
}
