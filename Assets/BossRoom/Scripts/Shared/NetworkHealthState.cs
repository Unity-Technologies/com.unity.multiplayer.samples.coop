using MLAPI;
using MLAPI.NetworkedVar;
using UnityEngine;

namespace BossRoom
{
    public class NetworkHealthState : NetworkedBehaviour
    {
        /// <summary>
        /// Current HP. This value is populated at startup time from CharacterClass data.
        /// </summary>
        public NetworkedVarInt HitPoints;

        [SerializeField]
        int m_MaxHealth;

        public override void NetworkStart()
        {
            HitPoints = GetComponent<NetworkCharacterState>().HitPoints;
            HealthBarManager.Instance.AddHealthState(NetworkedObject.NetworkId, this, m_MaxHealth);

            if (!IsClient)
            {
                enabled = false;
            }
        }
    }
}
