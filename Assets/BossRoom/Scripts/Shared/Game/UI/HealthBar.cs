using UnityEngine;
using UnityEngine.UI;

namespace BossRoom
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField]
        Slider hitPointsSlider;

        public void InitializeSlider(int maxValue, int minValue = 0)
        {
            hitPointsSlider.minValue = minValue;
            hitPointsSlider.maxValue = maxValue;
            hitPointsSlider.value = maxValue;
        }

        public void SetHitPoints(int hitPoints)
        {
            hitPointsSlider.value = hitPoints;
        }
    }
}
