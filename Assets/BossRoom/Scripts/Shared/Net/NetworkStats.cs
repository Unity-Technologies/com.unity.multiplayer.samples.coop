using System;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using UnityEngine;
using UnityEngine.UI;

/*
 * This utility help showing Network statistics at runtime.
 *
 * This component attaches to any networked object.
 * It'll spawn all the needed text and canvas.
 */
[RequireComponent(typeof(NetworkedObject))]
public class NetworkStats : NetworkedBehaviour
{
    /*
     * RTT
     * Client sends a ping RPC to the server and starts it's timer.
     * The server receives the ping and sends a pong response to the client.
     * The client receives that pong response and stops its time.
     * The RPC value is using a moving average, so we don't have a value that moves too much, but is still reactive to RTT changes.
     *
     * Note: when adding more stats, it might be worth it to abstract these in their own classes instead of having a bunch
     * of attributes floating around.
     */

    // Moving average attributes
    Queue<float> m_MovingWindow = new Queue<float>();
    const int k_MaxWindowSizeSeconds = 3; // it should take x seconds for the value to react to change
    float m_MaxWindowSize => k_MaxWindowSizeSeconds / m_PingIntervalSeconds;

    // RTT configurations
    [SerializeField]
    [Tooltip("The interval to send ping RPCs to calculate the RTT. The bigger the number, the less reactive the stat will be to RTT changes")]
    float m_PingIntervalSeconds = 0.1f;

    float m_LastPingTime;

    public float LastRTT { get; private set; }

    Text m_RTTText;

    // When receiving pong client RPCs, we need to know when the initiating ping sent it so we can calculate its individual RTT
    int m_CurrentRTTPingId;
    Dictionary<int, float> m_PingHistoryStartTimes = new Dictionary<int, float>();

    void Awake()
    {
        if (!Debug.isDebugBuild)
        {
            Destroy(this);
        }
    }

    public override void NetworkStart()
    {
        base.NetworkStart();

        bool IsClientOnly = IsClient && !IsServer;

        if ((!IsServer && !IsClientOnly) || (!IsOwner && IsClientOnly)) // !IsOwner since we don't care about the other player's stats
        {
            Destroy(this);
            return;
        }

        CreateTextOverlay();
    }

    // Creating our own canvas so this component is easy to add and remove from the project
    void CreateTextOverlay()
    {
        GameObject canvasGameObject = new GameObject("Debug Overlay Canvas");
        var canvas = canvasGameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = Camera.current;

        GameObject rttText = new GameObject("RTT Text");
        rttText.transform.SetParent(canvas.transform);
        m_RTTText = rttText.AddComponent<Text>();
        m_RTTText.text = "RTT: 0 ms";
        m_RTTText.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        m_RTTText.horizontalOverflow = HorizontalWrapMode.Overflow;

        var rectTransform = rttText.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2();
        rectTransform.anchorMax = new Vector2();
        rectTransform.position = new Vector3(rectTransform.rect.width / 2, 0);
    }

    void FixedUpdate()
    {
        if (!IsServer)
        {
            if (Time.realtimeSinceStartup - m_LastPingTime > m_PingIntervalSeconds)
            {
                // We could have had a ping/pong where the ping sends the pong and the pong sends the ping. Issue with this
                // is the higher the latency, the lower the sampling would be. We need pings to be sent at a regular interval
                InvokeServerRpc<int>(PingServerRPC, m_CurrentRTTPingId);
                m_PingHistoryStartTimes[m_CurrentRTTPingId] = Time.realtimeSinceStartup;
                m_CurrentRTTPingId++;
                m_LastPingTime = Time.realtimeSinceStartup;
            }
        }
    }

    [ServerRPC]
    public void PingServerRPC(int pingId)
    {
        InvokeClientRpcOnOwner<int>(PongClientRPC, pingId);
    }

    [ClientRPC]
    public void PongClientRPC(int pingId)
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

        float RTTSum = 0;
        foreach (var singleRTT in m_MovingWindow)
        {
            RTTSum += singleRTT;
        }

        LastRTT = RTTSum / m_MaxWindowSize;
        if (m_RTTText != null)
        {
            m_RTTText.text = $"RTT: {LastRTT * 1000f} ms";
        }
    }
}
