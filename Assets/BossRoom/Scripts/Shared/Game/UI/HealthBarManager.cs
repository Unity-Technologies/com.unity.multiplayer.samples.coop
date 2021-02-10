using System;
using System.Collections.Generic;
using MLAPI.NetworkedVar;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// Class designed to keep track of and position in UI-space HealthBar objects.
    /// </summary>
    public class HealthBarManager : MonoBehaviour
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

        /// <summary>
        /// Creates a HealthBar object in UI-space to display a NetworkedObject's health.
        /// </summary>
        /// <param name="networkID"> Network ID of this NetworkedObject. </param>
        /// <param name="networkedHealth"> Health as Networked Int. </param>
        /// <param name="maxHP"> Max Health value of a health bar. </param>
        /// <param name="trackedTransform"> Health bar will appear at this Transform. </param>
        public void AddHealthBar(ulong networkID, NetworkedVarInt networkedHealth, int maxHP,
            Transform trackedTransform)
        {
            if (m_HealthItems.ContainsKey(networkID))
            {
                return;
            }

            var healthItem = new HealthItem();
            var healthBarClone = Instantiate(m_HealthBarPrefab, transform);
            healthBarClone.InitializeSlider(networkedHealth, maxHP);
            healthItem.HealthBar = healthBarClone;
            healthItem.TrackedTransform = trackedTransform;
            m_HealthItems.Add(networkID, healthItem);
        }

        /// <summary>
        /// Removes a NetworkedObject's HealthBar.
        /// </summary>
        /// <param name="networkID"></param>
        public void RemoveHealthBar(ulong networkID)
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
            }
        }

        /// <summary>
        /// Container struct for components to track or modify per UI health bar.
        /// </summary>
        struct HealthItem
        {
            public HealthBar HealthBar;
            public Transform TrackedTransform;
        }
    }
}
