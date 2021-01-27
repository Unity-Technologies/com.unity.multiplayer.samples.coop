using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using UnityEngine;
using UnityEngine.UI;

/*
 * This component attaches to the player object.
 * It'll spawn all the needed text and canvas
 */
public class NetworkStats : NetworkedBehaviour
{
    Queue<float> m_MovingWindow = new Queue<float>();
    const int k_MaxWindowSizeSeconds = 3; // it should take x seconds for the value to change
    private float m_MaxWindowSize => k_MaxWindowSizeSeconds / m_PingIntervalSeconds;

    [SerializeField]
    float m_PingIntervalSeconds = 0.1f;
    float m_LastPingTime = 0f;

    public float LastRTT { get; private set; }

    Text m_RTTText;
    int m_CurrentId;
    Dictionary<int, float> m_PingHistoryStartTimes = new Dictionary<int, float>();

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

    private void CreateTextOverlay()
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
                InvokeServerRpc<int>(PingServerRPC, m_CurrentId);
                m_PingHistoryStartTimes[m_CurrentId] = Time.realtimeSinceStartup;
                m_CurrentId++;
                m_LastPingTime = Time.realtimeSinceStartup;
            }
        }
    }

    [ServerRPC]
    public void PingServerRPC(int pingId)
    {
        Debug.Log("ping");
        InvokeClientRpcOnOwner<int>(PongClientRPC, pingId);
    }

    [ClientRPC]
    public void PongClientRPC(int pingId)
    {
        // can't call ping from the pong, since higher latencies would mean we have a lower sample rate. We need to call ping regularly.
        Debug.Log("pong");

        var startTime = m_PingHistoryStartTimes[pingId];
        m_PingHistoryStartTimes.Remove(pingId);
        m_MovingWindow.Enqueue(Time.realtimeSinceStartup - startTime);
        UpdateRTTAverage();
    }

    private void UpdateRTTAverage()
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
            m_RTTText.text = $"RTT: {LastRTT}";
        }
    }
}
