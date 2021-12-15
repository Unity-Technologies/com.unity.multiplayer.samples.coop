using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;


namespace Unity.Multiplayer.Samples.Bossroom
{
    /// <summary>
    /// This script handles setting different settings for the camera based on quality level.
    /// </summary>
    public class CameraSettings : MonoBehaviour
    {
        public Camera mainCamera;
        
        public void HandleQualitySettings()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            
            int qualityLevel = QualitySettings.GetQualityLevel();

            if (qualityLevel == 2)
            {
                mainCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            }
            else
            {
                mainCamera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            }
        }
    }
}