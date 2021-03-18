using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace BossRoom.Visual
{
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        private CinemachineFreeLook m_MainCamera;
        private Coroutine m_CoroCameraShake;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            AttachCamera();
        }

        private void AttachCamera()
        {
            m_MainCamera = GameObject.FindObjectOfType<CinemachineFreeLook>();
            Assert.IsNotNull(m_MainCamera, "CameraController.AttachCamera: Couldn't find gameplay freelook camera");

            if (m_MainCamera)
            {
                // camera body / aim 
                m_MainCamera.Follow = transform;
                m_MainCamera.LookAt = transform;
                // default rotation / zoom
                m_MainCamera.m_Heading.m_Bias = 40f;
                m_MainCamera.m_YAxis.Value = 0.5f;
            }
        }

        public void ShakeCamera(float frequency, float amplitude, float durationSecs)
        {
            if (m_CoroCameraShake != null)
            {
                StopCoroutine(m_CoroCameraShake);
            }
            m_CoroCameraShake = StartCoroutine(CoroShakeCamera(frequency, amplitude, durationSecs));
        }

        private IEnumerator CoroShakeCamera(float frequency, float amplitude, float durationSecs)
        {
            // find the noise-generating components on each rig
            var perlins = new List<CinemachineBasicMultiChannelPerlin>();
            for (int i = 0; i < CinemachineFreeLook.RigNames.Length; ++i)
            {
                var rig = m_MainCamera.GetRig(i);
                var component = rig.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                if (!component)
                    Debug.LogError($"Virtual Camera's {CinemachineFreeLook.RigNames[i]} layer needs to have Noise set to Basic Multi Channel Perlin", m_MainCamera.gameObject);
                else
                    perlins.Add(component);
            }

            // make 'em shake
            foreach (var perlin in perlins)
            {
                perlin.m_FrequencyGain = frequency;
                perlin.m_AmplitudeGain = amplitude;
            }

            yield return new WaitForSeconds(durationSecs);

            // turn shaking off
            foreach (var perlin in perlins)
            {
                perlin.m_FrequencyGain = 0;
                perlin.m_AmplitudeGain = 0;
            }
            m_CoroCameraShake = null;
        }

    }
}
