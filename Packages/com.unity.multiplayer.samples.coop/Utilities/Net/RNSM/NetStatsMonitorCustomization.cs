using System;
using Unity.Multiplayer.Tools.NetStatsMonitor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity.Multiplayer.Samples.Utilities
{
    public class NetStatsMonitorCustomization : MonoBehaviour
    {
        [SerializeField]
        RuntimeNetStatsMonitor m_Monitor;

        [SerializeField]
        InputActionReference m_ToggleNetworkStatsAction;

        void Start()
        {
            m_Monitor.Visible = false;

            m_ToggleNetworkStatsAction.action.performed += OnToggleNetworkStatsActionperformed;
        }

        void OnDestroy()
        {
            m_ToggleNetworkStatsAction.action.performed -= OnToggleNetworkStatsActionperformed;
        }

        void OnToggleNetworkStatsActionperformed(InputAction.CallbackContext obj)
        {
            m_Monitor.Visible = !m_Monitor.Visible; // toggle. Using "Visible" instead of "Enabled" to make sure RNSM keeps updating in the background
            // while not visible. This way, when bring it back visible, we can make sure values are up to date.
        }
    }
}
