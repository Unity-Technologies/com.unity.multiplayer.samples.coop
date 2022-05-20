using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// This utility help showing Network statistics at runtime.
    ///
    /// This component attaches to any networked object.
    /// It'll spawn all the needed text and canvas.
    ///
    /// NOTE: This class will be removed once Unity provides support for this.
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkStats : NetworkBehaviour
    {
        // For a value like RTT an exponential moving average is a better indication of the current rtt and fluctuates less.
        struct ExponentialMovingAverageCalculator
        {
            readonly float m_Alpha;
            float m_Average;

            public float Average => m_Average;

            public ExponentialMovingAverageCalculator(float average)
            {
                m_Alpha = 2f / (k_MaxWindowSize + 1);
                m_Average = average;
            }

            public float NextValue(float value) => m_Average = (value - m_Average) * m_Alpha + m_Average;
        }

        // RTT
        // Client sends a ping RPC to the server and starts it's timer.
        // The server receives the ping and sends a pong response to the client.
        // The client receives that pong response and stops its time.
        // The RPC value is using a moving average, so we don't have a value that moves too much, but is still reactive to RTT changes.

        const int k_MaxWindowSizeSeconds = 3; // it should take x seconds for the value to react to change
        const float k_PingIntervalSeconds = 0.1f;
        const float k_MaxWindowSize = k_MaxWindowSizeSeconds / k_PingIntervalSeconds;

        ExponentialMovingAverageCalculator m_BossRoomRTT = new ExponentialMovingAverageCalculator(0);
        ExponentialMovingAverageCalculator m_UtpRTT = new ExponentialMovingAverageCalculator(0);

        float m_LastPingTime;
        TextMeshProUGUI m_TextStat;
        TextMeshProUGUI m_TextHostType;

        // When receiving pong client RPCs, we need to know when the initiating ping sent it so we can calculate its individual RTT
        int m_CurrentRTTPingId;

        Dictionary<int, float> m_PingHistoryStartTimes = new Dictionary<int, float>();

        ClientRpcParams m_PongClientParams;

        string m_TextToDisplay;

        public override void OnNetworkSpawn()
        {
            bool isClientOnly = IsClient && !IsServer;
            if (!IsOwner && isClientOnly) // we don't want to track player ghost stats, only our own
            {
                Destroy(this);
                return;
            }

            if (IsOwner)
            {
                CreateNetworkStatsText();
            }

            m_PongClientParams = new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = new[] { OwnerClientId } } };
        }

        // Creating a UI text object and add it to NetworkOverlay canvas
        void CreateNetworkStatsText()
        {
            Assert.IsNotNull(Editor.NetworkOverlay.Instance,
                "No NetworkOverlay object part of scene. Add NetworkOverlay prefab to bootstrap scene!");

            string hostType = IsHost ? "Host" : IsClient ? "Client" : "Unknown";
            InitializeTextLine($"Type: {hostType}", out m_TextHostType);
            InitializeTextLine("No Stat", out m_TextStat);
        }

        void InitializeTextLine(string defaultText, out TextMeshProUGUI textComponent)
        {
            var rootGO = new GameObject("UI Stat Text");
            textComponent = rootGO.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 24;
            textComponent.text = defaultText;
            textComponent.horizontalAlignment = HorizontalAlignmentOptions.Left;
            textComponent.verticalAlignment = VerticalAlignmentOptions.Middle;
            textComponent.raycastTarget = false;
            textComponent.autoSizeTextContainer = true;

            var rectTransform = rootGO.GetComponent<RectTransform>();
            Editor.NetworkOverlay.Instance.AddToUI(rectTransform);
        }

        void FixedUpdate()
        {
            if (!IsServer)
            {
                if (Time.realtimeSinceStartup - m_LastPingTime > k_PingIntervalSeconds)
                {
                    // We could have had a ping/pong where the ping sends the pong and the pong sends the ping. Issue with this
                    // is the higher the latency, the lower the sampling would be. We need pings to be sent at a regular interval
                    PingServerRPC(m_CurrentRTTPingId);
                    m_PingHistoryStartTimes[m_CurrentRTTPingId] = Time.realtimeSinceStartup;
                    m_CurrentRTTPingId++;
                    m_LastPingTime = Time.realtimeSinceStartup;

                    m_UtpRTT.NextValue(NetworkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId));
                }

                if (m_TextStat != null)
                {
                    m_TextToDisplay = $"RTT: {(m_BossRoomRTT.Average * 1000).ToString("0")} ms;\nUTP RTT {m_UtpRTT.Average.ToString("0")} ms";
                }
            }
            else
            {
                m_TextToDisplay = $"Connected players: {NetworkManager.Singleton.ConnectedClients.Count.ToString()}";
            }

            if (m_TextStat)
            {
                m_TextStat.text = m_TextToDisplay;
            }
        }

        [ServerRpc]
        void PingServerRPC(int pingId, ServerRpcParams serverParams = default)
        {
            PongClientRPC(pingId, m_PongClientParams);
        }

        [ClientRpc]
        void PongClientRPC(int pingId, ClientRpcParams clientParams = default)
        {
            var startTime = m_PingHistoryStartTimes[pingId];
            m_PingHistoryStartTimes.Remove(pingId);
            m_BossRoomRTT.NextValue(Time.realtimeSinceStartup - startTime);
        }

        public override void OnNetworkDespawn()
        {
            if (m_TextStat != null)
            {
                Destroy(m_TextStat.gameObject);
            }

            if (m_TextHostType != null)
            {
                Destroy(m_TextHostType.gameObject);
            }
        }
    }
}
