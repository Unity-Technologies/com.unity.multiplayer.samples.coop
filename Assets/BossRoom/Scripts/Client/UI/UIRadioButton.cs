using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BossRoom.Client
{
    /// <summary>
    /// Class inheriting from Button to mimic a radio button. When this is selected via UI, the graphics element is
    /// modified internally.
    /// </summary>
    public class UIRadioButton : Button
    {
        [SerializeField]
        Image m_IsOnGraphic;

        bool m_IsOn;

        public bool IsOn
        {
            set
            {
                m_IsOn = value;
                IsOnChanged();
            }
        }

        /// <summary>
        /// Called when the user clicks up on the button (completing a click event)
        /// </summary>
        public event Action OnPointerUpRaised;

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (!IsInteractable())
            {
                return;
            }

            base.OnPointerUp(eventData);
            IsOn = true;
            OnPointerUpRaised?.Invoke();
        }

        void IsOnChanged()
        {
            m_IsOnGraphic.enabled = m_IsOn;
        }
    }
}

