using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BossRoom.Visual
{
    public class HeroActionButton : Button, IPointerDownHandler, IPointerUpHandler
    {
        public float DownScale = 0.95f;
        //Renderer rend;

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            transform.localScale = new Vector3(DownScale, DownScale, DownScale);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            transform.localScale = new Vector3(1f, 1f, 1f);
        }

        public bool IsDown()
        {
            return IsPressed();
        }
    }
}

