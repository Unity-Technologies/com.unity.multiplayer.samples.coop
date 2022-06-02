using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// This script randomly varies a light source to create a flickering effect.
    /// </summary>
    public class RandomizedLight : MonoBehaviour
    {
        private const int k_IntensityScale = 100;

        [Tooltip("External light to vary. Leave null if this script is itself attached to a Light")]
        public Light m_TargetLight;

        [Tooltip("Minimum light intensity to randomize to")]
        public float m_MinIntensity = 0f;

        [Tooltip("Maximum light intensity to randomize to")]
        public float m_MaxIntensity = 1f;

        [Tooltip("How much smoothing to apply to the signal. Lower values will be less smoothed.")]
        [Range(1, 50)]
        public int m_Smoothing = 5;

        private int[] m_RingBuffer;   //a buffer full of noise ranging from min to max.
        private int m_RingSum;        //the sum of all the values in the current ring buffer.
        private int m_RingIndex;      //the current index of the buffer.

        // Start is called before the first frame update
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

        private void UpdateNoiseBuffer()
        {
            int newValue = (int)(Random.Range(m_MinIntensity, m_MaxIntensity) * k_IntensityScale);
            m_RingSum += (newValue - m_RingBuffer[m_RingIndex]);
            m_RingBuffer[m_RingIndex] = newValue;

            m_RingIndex = (m_RingIndex + 1) % m_RingBuffer.Length;
        }

        // Update is called once per frame
        void Update()
        {
            //should be a value between 0-1
            float lightIntensity = m_RingSum / (float)(m_RingBuffer.Length * k_IntensityScale);
            m_TargetLight.intensity = lightIntensity;

            UpdateNoiseBuffer();
        }
    }
}
