using UnityEngine;
using UnityEngine.UI;

namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// UI object that visually represents an object's health. Visuals are updated when NetworkVariable is modified.
    /// </summary>
    public class UIHealth : MonoBehaviour
    {
        [SerializeField]
        Slider m_HitPointsSlider;

        public void Initialize(int maxValue)
        {
            m_HitPointsSlider.minValue = 0;
            m_HitPointsSlider.maxValue = maxValue;
        }

        public void HealthChanged(int previousValue, int newValue)
        {
            m_HitPointsSlider.value = newValue;
            // disable slider when we're at full health!
            m_HitPointsSlider.gameObject.SetActive(m_HitPointsSlider.value != m_HitPointsSlider.maxValue);
        }
    }
}
