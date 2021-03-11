using Cinemachine;
using UnityEngine;
using UnityEngine.Assertions;

namespace BossRoom.Visual
{
    public class CameraController : MonoBehaviour
    {
        float m_MinZoomDistance = 3;
        float m_MaxZoomDistance = 30;
        float m_ZoomSpeed = 3;

        private CinemachineVirtualCamera m_MainCamera;

        void Start()
        {
            var visualization = GetComponent<ClientCharacterVisualization>();

            Assert.IsNotNull(visualization, "CameraController.Start: Couldn't find character visualization.");

            m_MinZoomDistance = visualization.MinZoomDistance;
            m_MaxZoomDistance = visualization.MaxZoomDistance;
            m_ZoomSpeed = visualization.ZoomSpeed;

            AttachCamera();
        }

        // Update is called once per frame
        void Update()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0.0f && m_MainCamera)
            {
                ZoomCamera(scroll);
            }
        }

        private void AttachCamera()
        {
            var cameraGO = GameObject.FindGameObjectWithTag("CMCamera");
            if (cameraGO == null)
            {
                return;
            }

            m_MainCamera = cameraGO.GetComponent<CinemachineVirtualCamera>();
            if (m_MainCamera)
            {
                m_MainCamera.Follow = m_MainCamera.LookAt = transform;
            }
        }

        private void ZoomCamera(float scroll)
        {
            CinemachineComponentBase[] components = m_MainCamera.GetComponentPipeline();
            foreach (CinemachineComponentBase component in components)
            {
                if (component is CinemachineFramingTransposer)
                {
                    CinemachineFramingTransposer c = (CinemachineFramingTransposer) component;
                    c.m_CameraDistance += -scroll * m_ZoomSpeed;
                    if (c.m_CameraDistance < m_MinZoomDistance)
                        c.m_CameraDistance = m_MinZoomDistance;
                    if (c.m_CameraDistance > m_MaxZoomDistance)
                        c.m_CameraDistance = m_MaxZoomDistance;
                }
            }
        }
    }
}
