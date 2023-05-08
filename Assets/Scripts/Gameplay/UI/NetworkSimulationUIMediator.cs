using System;
using System.Collections.Generic;
using TMPro;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.BossRoom.Gameplay.UI
{
    public class NetworkSimulationUIMediator : MonoBehaviour
    {
        [SerializeField]
        NetworkSimulator m_NetworkSimulator;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        TMP_Dropdown m_Dropdown;

        [SerializeField]
        KeyCode m_OpenWindowKeyCode = KeyCode.Tilde;

        const int k_NbTouchesToOpenWindow = 5;

        Dictionary<string, INetworkSimulatorPreset> m_SimulatorPresets = new Dictionary<string, INetworkSimulatorPreset>();

        void Start()
        {
            var optionData = new List<TMP_Dropdown.OptionData>();
            foreach (var networkSimulatorPreset in NetworkSimulatorPresets.Values)
            {
                m_SimulatorPresets[networkSimulatorPreset.Name] = networkSimulatorPreset;
                optionData.Add(new TMP_Dropdown.OptionData(networkSimulatorPreset.Name));
            }
            m_Dropdown.AddOptions(optionData);
            m_Dropdown.onValueChanged.AddListener(OnPresetChanged);
        }

        void OnPresetChanged(int optionIndex)
        {
            Debug.Log(m_Dropdown.options[optionIndex].text);
            m_NetworkSimulator.ChangeConnectionPreset(m_SimulatorPresets[m_Dropdown.options[optionIndex].text]);
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0;
        }

        void Show()
        {
            m_CanvasGroup.alpha = 1;
        }

        void ToggleVisibility()
        {
            if (m_CanvasGroup.alpha > 0)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        void Update()
        {
            if (Input.touchCount == k_NbTouchesToOpenWindow && AnyTouchDown() ||
                m_OpenWindowKeyCode != KeyCode.None && Input.GetKeyDown(m_OpenWindowKeyCode))
            {
                ToggleVisibility();
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
