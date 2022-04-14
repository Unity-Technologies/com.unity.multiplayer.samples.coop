using Unity.Multiplayer.Tools.NetStatsMonitor;
using UnityEngine;

public class NetStatsMonitorCustomization : MonoBehaviour
{
    [SerializeField]
    RuntimeNetStatsMonitor m_Monitor;

    void Start()
    {
        m_Monitor.enabled = false;
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.S))
        {
            m_Monitor.enabled = !m_Monitor.enabled; // toggle
        }
    }
}
