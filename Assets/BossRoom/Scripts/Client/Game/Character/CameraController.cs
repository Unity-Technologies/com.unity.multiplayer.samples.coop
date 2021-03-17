using Cinemachine;
using UnityEngine;
using UnityEngine.Assertions;

namespace BossRoom.Visual
{
    public class CameraController : MonoBehaviour
    {
        CinemachineFreeLook m_MainCamera;

        void Start()
        {
            AttachCamera();
        }

        void AttachCamera()
        {
            m_MainCamera = FindObjectOfType<CinemachineFreeLook>();
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
    }
}
