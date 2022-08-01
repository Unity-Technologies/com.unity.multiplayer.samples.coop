using Unity.Multiplayer.Tools.NetStatsMonitor;
using UnityEngine;

namespace Unity.Multiplayer.Samples.Utilities
{
    public class NetStatsMonitorCustomization : MonoBehaviour
    {
        [SerializeField]
        RuntimeNetStatsMonitor m_Monitor;

        const int k_NbTouchesToOpenWindow = 3;

        void Start()
        {
            m_Monitor.Visible = false;
        }

        void Update()
        {
            if (Input.GetKeyUp(KeyCode.S) || Input.touchCount == k_NbTouchesToOpenWindow && AnyTouchDown())
            {
                m_Monitor.Visible = !m_Monitor.Visible; // toggle
            }
        }

        static bool AnyTouchDown()
        {
            foreach (var touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
