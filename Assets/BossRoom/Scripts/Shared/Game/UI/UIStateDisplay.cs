using System;
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

        public void DisplayName(NetworkNameState networkNameState)
        {
            m_UIName.gameObject.SetActive(true);
            m_UIName.Initialize(networkNameState);
        }

        public void DisplayHealth(NetworkHealthState networkHealthState, int maxValue)
        {
            m_UIHealth.gameObject.SetActive(true);
            m_UIHealth.Initialize(networkHealthState, maxValue);
        }

        public void HideHealth()
        {
            m_UIHealth.gameObject.SetActive(false);
        }
    }
}
