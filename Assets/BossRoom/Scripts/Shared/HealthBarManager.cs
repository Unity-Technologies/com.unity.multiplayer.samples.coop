using System;
using System.Collections.Generic;
using MLAPI;
using UnityEngine;

namespace BossRoom
{
    public class HealthBarManager : NetworkedBehaviour
    {
        public static HealthBarManager Instance { get; private set; }

        [SerializeField]
        HealthBar m_HealthBarPrefab;

        Dictionary<ulong, NetworkHealthState> m_HealthStates;

        Dictionary<ulong, HealthBar> m_HealthBars;

        [SerializeField]
        Camera m_Camera;

        void Awake()
        {
            Instance = this;
            m_HealthStates = new Dictionary<ulong, NetworkHealthState>();
            m_HealthBars = new Dictionary<ulong, HealthBar>();
        }

        public void AddHealthState(ulong networkID, NetworkHealthState networkHealthState, int maxValue)
        {
            if (m_HealthStates.ContainsKey(networkID))
            {
                return;
            }

            var healthBarClone = Instantiate(m_HealthBarPrefab, transform);
            healthBarClone.InitializeSlider(maxValue);
            m_HealthStates.Add(networkID, networkHealthState);
            m_HealthBars.Add(networkID, healthBarClone);
        }

        public void Remove(ulong networkID)
        {
            m_HealthStates.Remove(networkID);
            m_HealthBars.Remove(networkID);
        }

        void Update()
        {
            foreach (KeyValuePair<ulong, NetworkHealthState> keyToHealthState in m_HealthStates)
            {
                m_HealthBars[keyToHealthState.Key].transform.position =
                    m_Camera.WorldToScreenPoint(keyToHealthState.Value.transform.position);

                m_HealthBars[keyToHealthState.Key].SetHitPoints(keyToHealthState.Value.HitPoints.Value);
            }
        }
    }
}
