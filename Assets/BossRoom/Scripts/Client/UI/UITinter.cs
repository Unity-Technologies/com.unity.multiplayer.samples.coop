using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameLobby.UI
{
    [RequireComponent(typeof(Image))]
    public class UITinter : MonoBehaviour
    {

        [SerializeField]
        Color[] m_TintColors;
        Image m_Image;
        void Awake()
        {
            m_Image = GetComponent<Image>();
        }

        public void SetToColor(bool firstTwoColors)
        {
            SetToColor(firstTwoColors ? 1 : 0);
        }

        public void SetToColor(int colorIndex)
        {
            if (colorIndex >= m_TintColors.Length)
                return;
            m_Image.color = m_TintColors[colorIndex];
        }

    }


}
