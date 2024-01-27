using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Multiplayer.Samples.BossRoom
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI informationText;
        [SerializeField] private TextMeshProUGUI weightText;

        [SerializeField] private Image[] inventorySlotImage;
        [SerializeField] private Image hpGauge;
        [SerializeField] private Image staminaGauge;


        public void Update()
        {
            foreach (var image in inventorySlotImage)
            {
                if (image.sprite == null)
                {
                    image.enabled = false;
                }
                else
                {
                    image.enabled = true;
                }
            }
            
        }

        public void SetTime(int h,int m)
        {
            timeText.SetText("{0} : {1}",h,m);    
        }

        public void SetInformation(string text)
        {
            informationText.SetText(text);
        }

        public void SetWeight(float weight)
        {
            weightText.SetText("{0:F1}");
        }

        public void SetInventoryImage(int num,Sprite sprite)
        {
            inventorySlotImage[num].sprite=sprite;
        }

        public void SetHPRate(float hpRate)
        {
            hpGauge.fillAmount = hpRate;
        }

        public void SetStaminaRate(float staminaRate)
        {
            staminaGauge.fillAmount = staminaRate;
        }
    }
}
