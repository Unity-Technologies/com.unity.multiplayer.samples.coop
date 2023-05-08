using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using UnityEngine;

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
        TMP_InputField m_LagSpikeDuration;

        [SerializeField]
        KeyCode m_OpenWindowKeyCode = KeyCode.Tilde;

        const int k_NbTouchesToOpenWindow = 5;

        Dictionary<string, INetworkSimulatorPreset> m_SimulatorPresets = new Dictionary<string, INetworkSimulatorPreset>();

        bool m_Shown;

        void Awake()
        {
            var optionData = new List<TMP_Dropdown.OptionData>();
            foreach (var networkSimulatorPreset in NetworkSimulatorPresets.Values)
            {
                m_SimulatorPresets[networkSimulatorPreset.Name] = networkSimulatorPreset;
                optionData.Add(new TMP_Dropdown.OptionData(networkSimulatorPreset.Name));
            }
            m_Dropdown.AddOptions(optionData);
            m_Dropdown.onValueChanged.AddListener(OnPresetChanged);
            Hide();
        }

        void OnPresetChanged(int optionIndex)
        {
            Debug.Log(m_Dropdown.options[optionIndex].text);
            m_NetworkSimulator.ChangeConnectionPreset(m_SimulatorPresets[m_Dropdown.options[optionIndex].text]);
        }

        public void Hide()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.interactable = false;
            m_CanvasGroup.blocksRaycasts = false;
            m_Shown = false;
        }

        void Show()
        {
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.interactable = true;
            m_CanvasGroup.blocksRaycasts = true;
            m_Shown = true;
        }

        void ToggleVisibility()
        {
            if (m_Shown)
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
            if (m_NetworkSimulator.IsAvailable)
            {
                if (Input.touchCount == k_NbTouchesToOpenWindow && AnyTouchDown() ||
                    m_OpenWindowKeyCode != KeyCode.None && Input.GetKeyDown(m_OpenWindowKeyCode))
                {
                    ToggleVisibility();
                }
            }
            else
            {
                if (m_Shown)
                {
                    Hide();
                }
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

        public void SimulateDisconnect()
        {
            m_NetworkSimulator.Disconnect();
        }

        public void TriggerLagSpike()
        {
            double.TryParse(m_LagSpikeDuration.text, out var duration);
            m_NetworkSimulator.TriggerLagSpike(TimeSpan.FromMilliseconds(duration));
        }

        public void SanitizeLagSpikeDurationInputField()
        {
            m_LagSpikeDuration.text = Regex.Replace(m_LagSpikeDuration.text, "[^0-9]", "");
        }
    }
}
