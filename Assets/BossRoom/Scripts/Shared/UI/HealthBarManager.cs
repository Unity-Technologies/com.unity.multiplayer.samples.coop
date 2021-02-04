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

        Dictionary<ulong, HealthItem> m_HealthItems;

        [SerializeField]
        Camera m_Camera;

        void Awake()
        {
            Instance = this;
            m_HealthItems = new Dictionary<ulong, HealthItem>();
        }

        public void AddHealthState(ulong networkID, NetworkHealthState networkHealthState,
            Transform transformToTrack = null)
        {
            if (m_HealthItems.ContainsKey(networkID))
            {
                return;
            }

            var healthItem = new HealthItem();
            var healthBarClone = Instantiate(m_HealthBarPrefab, transform);
            healthBarClone.InitializeSlider(networkHealthState.HitPoints.Value);
            healthItem.HealthBar = healthBarClone;
            healthItem.HealthState = networkHealthState;
            healthItem.TrackedTransform = transformToTrack == null ? networkHealthState.transform : transformToTrack;
            m_HealthItems.Add(networkID, healthItem);
        }

        public void Remove(ulong networkID)
        {
            var found = m_HealthItems.TryGetValue(networkID, out var healthItem);
            if (found)
            {
                Destroy(healthItem.HealthBar.gameObject);
            }

            m_HealthItems.Remove(networkID);
        }

        void FixedUpdate()
        {
            foreach (var keyToHealthItem in m_HealthItems)
            {
                keyToHealthItem.Value.HealthBar.transform.position =
                    m_Camera.WorldToScreenPoint(keyToHealthItem.Value.TrackedTransform.position);

                keyToHealthItem.Value.HealthBar.SetHitPoints(keyToHealthItem.Value.HealthState.HitPoints.Value);
            }
        }

        /// <summary>
        /// Container struct for components to track or modify per UI health bar
        /// </summary>
        struct HealthItem
        {
            public NetworkHealthState HealthState;
            public HealthBar HealthBar;
            public Transform TrackedTransform;
        }
    }
}
