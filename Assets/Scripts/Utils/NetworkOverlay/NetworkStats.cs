using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.BossRoom.Utils
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

        // Some games are less sensitive to latency than others. For fast-paced games, latency above 100ms becomes a challenge for players while for others 500ms is fine. It's up to you to establish those thresholds.
        const float k_StrugglingNetworkConditionsRTTThreshold = 130;
        const float k_BadNetworkConditionsRTTThreshold = 200;

        ExponentialMovingAverageCalculator m_BossRoomRTT = new ExponentialMovingAverageCalculator(0);
        ExponentialMovingAverageCalculator m_UtpRTT = new ExponentialMovingAverageCalculator(0);

        float m_LastPingTime;
        TextMeshProUGUI m_TextStat;
        TextMeshProUGUI m_TextHostType;
        TextMeshProUGUI m_TextBadNetworkConditions;

        // When receiving pong client RPCs, we need to know when the initiating ping sent it so we can calculate its individual RTT
        int m_CurrentRTTPingId;

        Dictionary<int, float> m_PingHistoryStartTimes = new Dictionary<int, float>();

        RpcParams m_PongClientParams;

        string m_TextToDisplay;

        public override void OnNetworkSpawn()
        {
            bool isClientOnly = IsClient && !IsServer;
            if (!IsOwner && isClientOnly) // we don't want to track player ghost stats, only our own
            {
                enabled = false;
                return;
            }

            if (IsOwner)
            {
                CreateNetworkStatsText();
            }

            m_PongClientParams = RpcTarget.Group(new[] { OwnerClientId }, RpcTargetUse.Persistent);
        }

        // Creating a UI text object and add it to NetworkOverlay canvas
        void CreateNetworkStatsText()
        {
            Assert.IsNotNull(Editor.NetworkOverlay.Instance,
                "No NetworkOverlay object part of scene. Add NetworkOverlay prefab to bootstrap scene!");

            string hostType = IsHost ? "Host" : IsClient ? "Client" : "Unknown";
            Editor.NetworkOverlay.Instance.AddTextToUI("UI Host Type Text", $"Type: {hostType}", out m_TextHostType);
            Editor.NetworkOverlay.Instance.AddTextToUI("UI Stat Text", "No Stat", out m_TextStat);
            Editor.NetworkOverlay.Instance.AddTextToUI("UI Bad Conditions Text", "", out m_TextBadNetworkConditions);
        }

        void FixedUpdate()
        {
            if (!IsServer)
            {
                if (Time.realtimeSinceStartup - m_LastPingTime > k_PingIntervalSeconds)
                {
                    // We could have had a ping/pong where the ping sends the pong and the pong sends the ping. Issue with this
                    // is the higher the latency, the lower the sampling would be. We need pings to be sent at a regular interval
                    ServerPingRpc(m_CurrentRTTPingId);
                    m_PingHistoryStartTimes[m_CurrentRTTPingId] = Time.realtimeSinceStartup;
                    m_CurrentRTTPingId++;
                    m_LastPingTime = Time.realtimeSinceStartup;

                    m_UtpRTT.NextValue(NetworkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId));
                }

                if (m_TextStat != null)
                {
                    m_TextToDisplay = $"RTT: {(m_BossRoomRTT.Average * 1000).ToString("0")} ms;\nUTP RTT {m_UtpRTT.Average.ToString("0")} ms";
                    if (m_UtpRTT.Average > k_BadNetworkConditionsRTTThreshold)
                    {
                        m_TextStat.color = Color.red;
                    }
                    else if (m_UtpRTT.Average > k_StrugglingNetworkConditionsRTTThreshold)
                    {
                        m_TextStat.color = Color.yellow;
                    }
                    else
                    {
                        m_TextStat.color = Color.white;
                    }
                }

                if (m_TextBadNetworkConditions != null)
                {
                    // Right now, we only base this warning on UTP's RTT metric, but in the future we could watch for packet loss as well, or other metrics.
                    // This could be a simple icon instead of doing heavy string manipulations.
                    m_TextBadNetworkConditions.text = m_UtpRTT.Average > k_BadNetworkConditionsRTTThreshold ? "Bad Network Conditions Detected!" : "";
                    var color = Color.red;
                    color.a = Mathf.PingPong(Time.time, 1f);
                    m_TextBadNetworkConditions.color = color;
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

        [Rpc(SendTo.Server)]
        void ServerPingRpc(int pingId, RpcParams serverParams = default)
        {
            ClientPongRpc(pingId, m_PongClientParams);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        void ClientPongRpc(int pingId, RpcParams clientParams = default)
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

            if (m_TextBadNetworkConditions != null)
            {
                Destroy(m_TextBadNetworkConditions.gameObject);
            }
        }
    }
}
