using System;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects
{
    /// <summary>
    /// This script handles the logic for a simple "single-shot" breakable object like a pot, or
    /// other stationary items with arbitrary amounts of HP, like spawner-portal crystals.
    /// Visualization for these objects works by swapping a "broken" prefab at the moment of breakage. The broken prefab
    /// then handles the pesky details of actually falling apart.
    /// </summary>
    public class Breakable : NetworkBehaviour, IDamageable, ITargetable
    {
        [Header("Server Logic")]
        [SerializeField]
        [Tooltip("If left blank, this breakable effectively has 1 hit point")]
        IntVariable m_MaxHealth;

        [SerializeField]
        [Tooltip("If this breakable will have hit points, add a NetworkHealthState component to this GameObject")]
        NetworkHealthState m_NetworkHealthState;

        [SerializeField]
        Collider m_Collider;

        [SerializeField]
        [Tooltip("Indicate which special interaction behaviors are needed for this breakable")]
        IDamageable.SpecialDamageFlags m_SpecialDamageFlags;

        [Header("Visualization")]
        [SerializeField]
        GameObject m_BrokenPrefab;

        [SerializeField]
        [Tooltip("If set, will be used instead of BrokenPrefab when new players join, skipping transition effects.")]
        GameObject m_PrebrokenPrefab;

        [SerializeField]
        [Tooltip("We use this transform's position and rotation when creating the prefab. (Defaults to self)")]
        Transform m_BrokenPrefabPos;

        [SerializeField]
        GameObject[] m_UnbrokenGameObjects;

        /// <summary>
        /// Is the item broken or not?
        /// </summary>
        public bool IsBroken => m_NetworkHealthState.HitPoints.Value == 0;

        public event Action Broken;

        public bool IsNpc => true;

        public bool IsValidTarget => !IsBroken;

        GameObject m_CurrentBrokenVisualization;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                if (m_MaxHealth && m_NetworkHealthState)
                {
                    m_NetworkHealthState.HitPoints.Value = m_MaxHealth.Value;
                }
            }

            if (IsClient)
            {
                if (m_NetworkHealthState)
                {
                    m_NetworkHealthState.HitPoints.OnValueChanged += OnHPChanged;
                }

                if (IsBroken)
                {
                    PerformBreakVisualization(true);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsClient && m_NetworkHealthState)
            {
                m_NetworkHealthState.HitPoints.OnValueChanged -= OnHPChanged;
            }
        }

        public void ReceiveHitPoints(ServerCharacter inflicter, int hitPoints)
        {
            if (hitPoints < 0)
            {
                if (inflicter && !inflicter.IsNpc)
                {
                    bool isNotDamagedByPlayers = (GetSpecialDamageFlags() & IDamageable.SpecialDamageFlags.NotDamagedByPlayers) == IDamageable.SpecialDamageFlags.NotDamagedByPlayers;
                    if (isNotDamagedByPlayers)
                    {
                        // a player tried to damage us, but we are immune to player damage!
                        return;
                    }
                }

                if (m_NetworkHealthState && m_MaxHealth)
                {
                    m_NetworkHealthState.HitPoints.Value =
                        Mathf.Clamp(m_NetworkHealthState.HitPoints.Value + hitPoints, 0, m_MaxHealth.Value);
                }
            }
        }

        public int GetTotalDamage()
        {
            return Math.Max(0, m_MaxHealth.Value - m_NetworkHealthState.HitPoints.Value);
        }

        public void Break()
        {
            m_NetworkHealthState.HitPoints.Value = 0;
        }

        public void Unbreak()
        {
            m_NetworkHealthState.HitPoints.Value = m_MaxHealth.Value;
        }

        public IDamageable.SpecialDamageFlags GetSpecialDamageFlags()
        {
            return m_SpecialDamageFlags;
        }

        public bool IsDamageable()
        {
            // you can damage this breakable until it's broken!
            return !IsBroken;
        }

        void OnHPChanged(int previousValue, int newValue)
        {
            if (IsServer)
            {
                if (m_Collider)
                {
                    m_Collider.enabled = !IsBroken;
                }
            }

            if (previousValue > 0 && newValue >= 0)
            {
                Broken?.Invoke();
                PerformBreakVisualization(false);
            }
            else if (previousValue == 0 && newValue > 0)
            {
                PerformUnbreakVisualization();
            }
        }

        void PerformBreakVisualization(bool onStart)
        {
            foreach (var unbrokenGameObject in m_UnbrokenGameObjects)
            {
                if (unbrokenGameObject)
                {
                    unbrokenGameObject.SetActive(false);
                }
            }

            if (m_CurrentBrokenVisualization)
                Destroy(m_CurrentBrokenVisualization); // just a safety check, should be null when we get here

            GameObject brokenPrefab = (onStart && m_PrebrokenPrefab != null) ? m_PrebrokenPrefab : m_BrokenPrefab;
            if (brokenPrefab)
            {
                m_CurrentBrokenVisualization = Instantiate(brokenPrefab, m_BrokenPrefabPos.position, m_BrokenPrefabPos.rotation, transform);
            }
        }

        void PerformUnbreakVisualization()
        {
            if (m_CurrentBrokenVisualization)
            {
                Destroy(m_CurrentBrokenVisualization);
            }

            foreach (var unbrokenGameObject in m_UnbrokenGameObjects)
            {
                if (unbrokenGameObject)
                {
                    unbrokenGameObject.SetActive(true);
                }
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!m_Collider)
                m_Collider = GetComponent<Collider>();
            if (!m_NetworkHealthState)
                m_NetworkHealthState = GetComponent<NetworkHealthState>();
            if (!m_BrokenPrefabPos)
                m_BrokenPrefabPos = transform;
        }
#endif
    }
}
