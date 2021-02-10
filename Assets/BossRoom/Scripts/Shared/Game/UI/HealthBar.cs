using System;
using MLAPI.NetworkedVar;
using UnityEngine;
using UnityEngine.UI;

namespace BossRoom
{
    /// <summary>
    /// UI object that visually represents an object's health. Slider value updated when health NetworkedVar is
    /// modified.
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [SerializeField]
        Slider m_HitPointsSlider;

        NetworkedVarInt m_NetworkedHealth;

        public void InitializeSlider(NetworkedVarInt networkedHealth, int maxValue)
        {
            m_NetworkedHealth = networkedHealth;

            m_HitPointsSlider.minValue = 0;
            m_HitPointsSlider.maxValue = maxValue;
            m_HitPointsSlider.value = maxValue;

            m_NetworkedHealth.OnValueChanged += HealthChanged;
        }

        void HealthChanged(int previousValue, int newValue)
        {
            m_HitPointsSlider.value = newValue;
        }

        void OnDestroy()
        {
            m_NetworkedHealth.OnValueChanged -= HealthChanged;
        }
    }
}
