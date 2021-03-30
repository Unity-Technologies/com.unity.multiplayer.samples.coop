using System;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// Class containing references to UI children that we can display. Both are disabled by default on prefab.
    /// </summary>
    public class UIStateDisplay : MonoBehaviour
    {
        [SerializeField]
        UIName m_UIName;

        [SerializeField]
        UIHealth m_UIHealth;

        public void DisplayName(NetworkVariableString networkedName)
        {
            m_UIName.gameObject.SetActive(true);
            m_UIName.Initialize(networkedName);
        }

        public void DisplayHealth(NetworkVariableInt networkedHealth, int maxValue)
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
