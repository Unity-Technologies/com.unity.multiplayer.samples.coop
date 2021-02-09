using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BossRoom.Visual
{
    /// <summary>
    /// Provides logic for a UI Button to slightly shrink scale on pointer down
    /// </summary>
    public class HeroActionButton : Button, IPointerDownHandler, IPointerUpHandler
    {
        private const float k_DownScale = 0.95f;
        //Renderer rend;

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            transform.localScale = new Vector3(k_DownScale, k_DownScale, k_DownScale);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
    }
}

