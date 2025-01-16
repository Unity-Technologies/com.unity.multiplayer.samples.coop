using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.BossRoom.CameraUtils
{
    public class CameraController : MonoBehaviour
    {
        const string k_CMCameraTag = "CMCamera";

        void Start()
        {
            AttachCamera();
        }

        void AttachCamera()
        {
            var cinemachineCameraGameObject = GameObject.FindGameObjectWithTag(k_CMCameraTag);
            Assert.IsNotNull(cinemachineCameraGameObject);

            var cinemachineCamera = cinemachineCameraGameObject.GetComponent<CinemachineCamera>();
            Assert.IsNotNull(cinemachineCamera, "CameraController.AttachCamera: Couldn't find gameplay CinemachineCamera");

            if (cinemachineCamera != null)
            {
                // camera body / aim
                cinemachineCamera.Follow = transform;
                cinemachineCamera.LookAt = transform;
            }

            var cinemachineOrbitalFollow = cinemachineCameraGameObject.GetComponent<CinemachineOrbitalFollow>();
            Assert.IsNotNull(cinemachineOrbitalFollow, "CameraController.AttachCamera: Couldn't find gameplay CinemachineOrbitalFollow");

            if (cinemachineOrbitalFollow != null)
            {
                // default rotation / zoom
                cinemachineOrbitalFollow.HorizontalAxis.Value = 40f;
                cinemachineOrbitalFollow.VerticalAxis.Value = 0.5f;
            }
        }
    }
}
