using System;
using MLAPI.NetworkedVar;
using UnityEngine;

namespace BossRoom
{
    public class UIStateDisplay : MonoBehaviour
    {
        [SerializeField]
        UIName m_UIName;

        [SerializeField]
        UIHealth m_UIHealth;

        public void DisplayName(NetworkedVarString networkedName)
        {
            m_UIName.gameObject.SetActive(true);
            m_UIName.Initialize(networkedName);
        }

        public void DisplayHealth(NetworkedVarInt networkedHealth, int maxValue)
        {
            m_UIHealth.gameObject.SetActive(true);
            m_UIHealth.Initialize(networkedHealth, maxValue);
        }

        public void HideHealth()
        {
            m_UIHealth.gameObject.SetActive(false);
        }
    }
}
