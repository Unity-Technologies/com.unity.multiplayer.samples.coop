using MLAPI;
using UnityEngine;

namespace BossRoom.Server
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

        [Tooltip("Indicate which special interaction behaviors are needed for this breakable")]
        IDamageable.SpecialDamageFlags m_SpecialDamageFlags;

        private NetworkBreakableState m_State;

        private void Awake()
        {
            m_State = GetComponent<NetworkBreakableState>();
        }

        public override void NetworkStart()
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

