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
        // We apply a uniform 95% scale to buttons when pressed
        static readonly Vector3 k_DownScale = new Vector3(0.95f, 0.95f, 0.95f);

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            transform.localScale = HeroActionButton.k_DownScale;
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            transform.localScale = Vector3.one;
        }
    }
}

