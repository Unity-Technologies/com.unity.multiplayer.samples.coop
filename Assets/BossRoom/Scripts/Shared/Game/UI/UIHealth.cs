using UnityEngine;
using UnityEngine.UI;

namespace BossRoom
{
    /// <summary>
    /// UI object that visually represents an object's health. Visuals are updated when NetworkVariable is modified.
    /// </summary>
    public class UIHealth : MonoBehaviour
    {
        [SerializeField]
        Slider m_HitPointsSlider;

        NetworkHealthState m_NetworkHealthState;

        public void Initialize(NetworkHealthState networkedHealth, int maxValue)
        {
            m_NetworkHealthState = networkedHealth;

            m_HitPointsSlider.minValue = 0;
            m_HitPointsSlider.maxValue = maxValue;
            HealthChanged(maxValue, maxValue);

            m_NetworkHealthState.AddListener(HealthChanged);
        }

        void HealthChanged(int previousValue, int newValue)
        {
            m_HitPointsSlider.value = newValue;
            // disable slider when we're at full health!
            m_HitPointsSlider.gameObject.SetActive(m_HitPointsSlider.value != m_HitPointsSlider.maxValue);
        }

        void OnDestroy()
        {
            m_NetworkHealthState.RemoveListener(HealthChanged);
        }
    }
}
