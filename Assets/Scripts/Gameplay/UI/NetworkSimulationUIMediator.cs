using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime.BuiltInScenarios;
using Unity.Netcode;
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
        TMP_Dropdown m_PresetsDropdown;

        [SerializeField]
        TMP_Dropdown m_ScenariosDropdown;

        [SerializeField]
        Button m_ScenariosButton;

        [SerializeField]
        TextMeshProUGUI m_ScenariosButtonText;

        [SerializeField]
        TMP_InputField m_LagSpikeDuration;

        [SerializeField]
        KeyCode m_OpenWindowKeyCode = KeyCode.Tilde;

        [SerializeField]
        ConnectionsCycle m_ConnectionsCycleScenario;

        [SerializeField]
        RandomConnectionsSwap m_RandomConnectionsSwapScenario;

        const int k_NbTouchesToOpenWindow = 5;

        Dictionary<string, INetworkSimulatorPreset> m_SimulatorPresets = new Dictionary<string, INetworkSimulatorPreset>();

        Dictionary<string, NetworkScenario> m_Scenarios = new Dictionary<string, NetworkScenario>();

        bool m_Shown;
        const string k_None = "None";
        const string k_PauseString = "Pause";
        const string k_ResumeString = "Resume";

        void Awake()
        {
            // initialize connection presets dropdown
            var optionData = new List<TMP_Dropdown.OptionData>();
            foreach (var networkSimulatorPreset in NetworkSimulatorPresets.Values)
            {
                m_SimulatorPresets[networkSimulatorPreset.Name] = networkSimulatorPreset;
                optionData.Add(new TMP_Dropdown.OptionData(networkSimulatorPreset.Name));
            }
            m_PresetsDropdown.AddOptions(optionData);
            m_PresetsDropdown.onValueChanged.AddListener(OnPresetChanged);

            // initialize scenario dropdown
            optionData = new List<TMP_Dropdown.OptionData>();

            // Adding empty scenario
            optionData.Add(new TMP_Dropdown.OptionData(k_None));

            // Adding ConnectionsCycle scenario
            var scenarioName = m_ConnectionsCycleScenario.GetType().Name;
            m_Scenarios[scenarioName] = m_ConnectionsCycleScenario;
            optionData.Add(new TMP_Dropdown.OptionData(scenarioName));

            // Adding RandomConnectionsSwap scenario
            scenarioName = m_RandomConnectionsSwapScenario.GetType().Name;
            m_Scenarios[scenarioName] = m_RandomConnectionsSwapScenario;
            optionData.Add(new TMP_Dropdown.OptionData(scenarioName));

            m_ScenariosDropdown.AddOptions(optionData);
            m_ScenariosDropdown.onValueChanged.AddListener(OnScenarioChanged);

            // Hide UI until ready
            Hide();
        }

        void Start()
        {
            NetworkManager.Singleton.OnClientStarted += OnNetworkManagerStarted;
            NetworkManager.Singleton.OnServerStarted += OnNetworkManagerStarted;
        }

        void OnDestroy()
        {
            if (NetworkManager.Singleton is not null)
            {
                NetworkManager.Singleton.OnClientStarted -= OnNetworkManagerStarted;
                NetworkManager.Singleton.OnServerStarted -= OnNetworkManagerStarted;
            }
        }

        void OnNetworkManagerStarted()
        {
            if (m_NetworkSimulator.IsAvailable)
            {
                Show();
            }
        }

        static bool TypeIsValidNetworkScenario(Type type)
        {
            return type.IsClass && type.IsAbstract == false && typeof(NetworkScenario).IsAssignableFrom(type);
        }

        void OnPresetChanged(int optionIndex)
        {
            m_NetworkSimulator.ChangeConnectionPreset(m_SimulatorPresets[m_PresetsDropdown.options[optionIndex].text]);
        }

        void OnScenarioChanged(int optionIndex)
        {
            var scenarioName = m_ScenariosDropdown.options[optionIndex].text;
            m_NetworkSimulator.Scenario = m_Scenarios.ContainsKey(scenarioName) ? m_Scenarios[scenarioName] : null;
            m_NetworkSimulator.Scenario?.Start(m_NetworkSimulator);
            UpdateScenarioButton();
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
            UpdateScenarioButton();
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

                var selectedPreset = m_PresetsDropdown.options[m_PresetsDropdown.value].text;
                if (selectedPreset != m_NetworkSimulator.CurrentPreset.Name)
                {
                    for (var i = 0; i < m_PresetsDropdown.options.Count; i++)
                    {
                        if (m_PresetsDropdown.options[i].text == m_NetworkSimulator.CurrentPreset.Name)
                        {
                            m_PresetsDropdown.value = i;
                        }
                    }
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

        public void TriggerScenario()
        {
            if (m_NetworkSimulator.Scenario != null)
            {
                m_NetworkSimulator.Scenario.IsPaused = !m_NetworkSimulator.Scenario.IsPaused;
                UpdateScenarioButton();
            }
        }

        void UpdateScenarioButton()
        {
            if (m_NetworkSimulator.Scenario != null)
            {
                m_ScenariosButtonText.text = m_NetworkSimulator.Scenario.IsPaused ? k_ResumeString : k_PauseString;
                m_ScenariosButton.interactable = true;
            }
            else
            {
                m_ScenariosButtonText.text = "";
                m_ScenariosButton.interactable = false;
            }
        }
    }
}
