using UnityEngine;
using UnityEngine.UI;

namespace BossRoom
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField]
        Slider m_HitPointsSlider;

        public void InitializeSlider(int maxValue, int minValue = 0)
        {
            m_HitPointsSlider.minValue = minValue;
            m_HitPointsSlider.maxValue = maxValue;
            m_HitPointsSlider.value = maxValue;
        }

        public void SetHitPoints(int hitPoints)
        {
            m_HitPointsSlider.value = hitPoints;
        }
    }
}
