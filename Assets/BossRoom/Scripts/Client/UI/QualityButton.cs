using TMPro;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    public class QualityButton : MonoBehaviour
    {
        public TMP_Text qualityBtnText;

        private void Start()
        {
            int index = QualitySettings.GetQualityLevel();
            qualityBtnText.text = QualitySettings.names[index];
        }

        public void SetQualitySettings()
        {
            int qualityLevels = QualitySettings.names.Length - 1;
            int currentLevel = QualitySettings.GetQualityLevel();

            if (currentLevel < qualityLevels)
            {
                QualitySettings.IncreaseLevel();
                qualityBtnText.text = QualitySettings.names[QualitySettings.GetQualityLevel()];
            }
            else
            {
                QualitySettings.SetQualityLevel(0);
                qualityBtnText.text = QualitySettings.names[QualitySettings.GetQualityLevel()];
            }
        
        }
    }  
}


