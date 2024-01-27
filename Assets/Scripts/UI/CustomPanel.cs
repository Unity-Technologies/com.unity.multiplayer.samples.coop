using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PanicBuying
{
    public class CustomPanel : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        private bool _isOver;

        public void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (!_isOver)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isOver = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isOver = false;
        }

    }
}
