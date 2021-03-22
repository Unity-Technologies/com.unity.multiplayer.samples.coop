using UnityEngine;
using Random = UnityEngine.Random;

namespace BossRoom.Visual
{
    /// <summary>
    /// This script randomly varies a light source to create a flickering effect.
    /// </summary>
    public class RandomizedLight : MonoBehaviour
    {
        const int k_IntensityScale = 100;

        [SerializeField]
        [Tooltip("External light to vary. Leave null if this script is itself attached to a Light")]
        Light m_TargetLight;

        [SerializeField]
        [Tooltip("Minimum light intensity to randomize to")]
        float m_MinIntensity;

        [SerializeField]
        [Tooltip("Maximum light intensity to randomize to")]
        float m_MaxIntensity = 1f;

        [SerializeField]
        [Tooltip("How much smoothing to apply to the signal. Lower values will be less smoothed.")]
        [Range(1, 50)]
        int m_Smoothing = 5;

        int[] m_RingBuffer;   //a buffer full of noise ranging from min to max.
        int m_RingSum;        //the sum of all the values in the current ring buffer.
        int m_RingIndex;      //the current index of the buffer.

        void Start()
        {
            m_RingBuffer = new int[m_Smoothing];
            for (int i = 0; i < m_RingBuffer.Length; ++i)
            {
                UpdateNoiseBuffer();
            }

            if (m_TargetLight == null)
            {
                m_TargetLight = GetComponent<Light>();
            }
        }

        void UpdateNoiseBuffer()
        {
            int newValue = (int)(Random.Range(m_MinIntensity, m_MaxIntensity) * k_IntensityScale);
            m_RingSum += (newValue - m_RingBuffer[m_RingIndex]);
            m_RingBuffer[m_RingIndex] = newValue;

            m_RingIndex = (m_RingIndex + 1) % m_RingBuffer.Length;
        }

        void Update()
        {
            //should be a value between 0-1
            float lightIntensity = m_RingSum / (float)(m_RingBuffer.Length * k_IntensityScale);
            m_TargetLight.intensity = lightIntensity;

            UpdateNoiseBuffer();
        }
    }
}
