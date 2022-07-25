using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom
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

        public void DisplayName()
        {
            m_UIName.gameObject.SetActive(true);
        }

        public void DisplayHealth(int maxValue)
        {
            m_UIHealth.gameObject.SetActive(true);
            m_UIHealth.Initialize(maxValue);
        }

        public void HideHealth()
        {
            m_UIHealth.gameObject.SetActive(false);
        }

        public void NameChanged(string previousValue, string newValue)
        {
            m_UIName.NameUpdated(previousValue, newValue);
        }

        public void HitPointsChanged(int previousValue, int newValue)
        {
            m_UIHealth.HealthChanged(previousValue, newValue);
        }
    }
}
