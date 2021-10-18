using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    /// <summary>
    /// This script handles the logic for a simple "single-shot" breakable object like a pot, or
    /// other stationary items with arbitrary amounts of HP, like spawner-portal crystals.
    /// </summary>
    [RequireComponent(typeof(NetworkBreakableState))]
    public class ServerBreakableLogic : NetworkBehaviour, IDamageable
    {
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

        private NetworkBreakableState m_State;

        private void Awake()
        {
            m_State = GetComponent<NetworkBreakableState>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
            }
            else
            {
                if (m_MaxHealth && m_NetworkHealthState)
                {
                    m_NetworkHealthState.HitPoints.Value = m_MaxHealth.Value;
                }
            }
        }

        public void ReceiveHP(ServerCharacter inflicter, int HP)
        {
            if (HP < 0)
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

                if (m_NetworkHealthState)
                {
                    m_NetworkHealthState.HitPoints.Value = Mathf.Max(m_NetworkHealthState.HitPoints.Value + HP, 0);
                    if (m_NetworkHealthState.HitPoints.Value <= 0)
                    {
                        Break();
                    }
                }
                else
                {
                    //any damage at all is enough to slay me.
                    Break();
                }
            }
        }

        private void Break()
        {
            m_State.IsBroken.Value = true;
            if (m_Collider)
                m_Collider.enabled = false;
        }

        public void Unbreak()
        {
            m_State.IsBroken.Value = false;
            if (m_Collider)
                m_Collider.enabled = true;
            if (m_MaxHealth && m_NetworkHealthState)
                m_NetworkHealthState.HitPoints.Value = m_MaxHealth.Value;
        }

        public IDamageable.SpecialDamageFlags GetSpecialDamageFlags()
        {
            return m_SpecialDamageFlags;
        }

        public bool IsDamageable()
        {
            // you can damage this breakable until it's broken!
            return !m_State.IsBroken.Value;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!m_Collider)
                m_Collider = GetComponent<Collider>();
            if (!m_NetworkHealthState)
                m_NetworkHealthState = GetComponent<NetworkHealthState>();
        }
#endif
    }


}

