using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

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
        // RTT
        // Client sends a ping RPC to the server and starts it's timer.
        // The server receives the ping and sends a pong response to the client.
        // The client receives that pong response and stops its time.
        // The RPC value is using a moving average, so we don't have a value that moves too much, but is still reactive to RTT changes.
        //
        // Note: when adding more stats, it might be worth it to abstract these in their own classes instead of having a bunch
        // of attributes floating around.

        public float LastRTT { get; private set; }

        [SerializeField]
        [Tooltip("The interval to send ping RPCs to calculate the RTT. The bigger the number, the less reactive the stat will be to RTT changes")]
        float m_PingIntervalSeconds = 0.1f;
        float m_LastPingTime;
        Text m_TextStat;
        Text m_TextHostType;

        // When receiving pong client RPCs, we need to know when the initiating ping sent it so we can calculate its individual RTT
        int m_CurrentRTTPingId;

        Queue<float> m_MovingWindow = new Queue<float>();
        const int k_MaxWindowSizeSeconds = 3; // it should take x seconds for the value to react to change
        float m_MaxWindowSize => k_MaxWindowSizeSeconds / m_PingIntervalSeconds;
        Dictionary<int, float> m_PingHistoryStartTimes = new Dictionary<int, float>();

        ClientRpcParams m_PongClientParams;


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

        private void InitializeTextLine(string defaultText, out Text textComponent )
        {
            GameObject rootGO = new GameObject("UI Stat Text");
            textComponent = rootGO.AddComponent<Text>();
            textComponent.text = defaultText;
            textComponent.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
            textComponent.alignment = TextAnchor.MiddleLeft;
            textComponent.raycastTarget = false;
            textComponent.resizeTextForBestFit = true;

            var rectTransform = rootGO.GetComponent<RectTransform>();
            Editor.NetworkOverlay.Instance.AddToUI(rectTransform);
        }

        void FixedUpdate()
        {
            var textToDisplay = string.Empty;
            if (!IsServer)
            {
                if (Time.realtimeSinceStartup - m_LastPingTime > m_PingIntervalSeconds)
                {
                    // We could have had a ping/pong where the ping sends the pong and the pong sends the ping. Issue with this
                    // is the higher the latency, the lower the sampling would be. We need pings to be sent at a regular interval
                    PingServerRPC(m_CurrentRTTPingId);
                    m_PingHistoryStartTimes[m_CurrentRTTPingId] = Time.realtimeSinceStartup;
                    m_CurrentRTTPingId++;
                    m_LastPingTime = Time.realtimeSinceStartup;
                }

                if (m_TextStat != null)
                {
                    textToDisplay = $"{textToDisplay}RTT: {(LastRTT * 1000f).ToString()} ms ";
                }
            }

            if (IsServer)
            {
                textToDisplay = $"{textToDisplay}Connected players: {NetworkManager.Singleton.ConnectedClients.Count.ToString()} ";
            }

            if (m_TextStat)
            {
                m_TextStat.text = textToDisplay;
            }
        }


        [ServerRpc]
        public void PingServerRPC(int pingId, ServerRpcParams serverParams=default)
        {
            PongClientRPC(pingId, m_PongClientParams);
        }

        [ClientRpc]
        public void PongClientRPC(int pingId, ClientRpcParams clientParams=default)
        {
            var startTime = m_PingHistoryStartTimes[pingId];
            m_PingHistoryStartTimes.Remove(pingId);
            m_MovingWindow.Enqueue(Time.realtimeSinceStartup - startTime);
            UpdateRTTSlidingWindowAverage();
        }

        void UpdateRTTSlidingWindowAverage()
        {
            if (m_MovingWindow.Count > m_MaxWindowSize)
            {
                m_MovingWindow.Dequeue();
            }

            float rttSum = 0;
            foreach (var singleRTT in m_MovingWindow)
            {
                rttSum += singleRTT;
            }

            LastRTT = rttSum / m_MaxWindowSize;
        }

        public override void OnNetworkDespawn()
        {
            if (m_TextStat != null)
            {
                Destroy(m_TextStat.gameObject);
            }
            if( m_TextHostType != null )
            {
                Destroy(m_TextHostType.gameObject);
            }
        }
    }
}
