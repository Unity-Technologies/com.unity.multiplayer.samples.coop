using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// Handles the display of a single message in the message feed using UI Toolkit.
    /// </summary>
    public class UIMessageSlot : MonoBehaviour
    {
        [SerializeField]
        UIDocument messageItem;
        
        VisualElement m_RootElement; // Root VisualElement of the message slot
        Label m_MessageLabel; // Label to display the message text
        int m_HideDelay; // Time before the message fades out or hides
        bool isDisplaying; // Tracks if this message is actively being shown

        public bool IsDisplaying => isDisplaying;

        
        void Awake()
        {
            m_RootElement = messageItem.rootVisualElement;
        }
        public UIMessageSlot(VisualElement rootElement, int hideDelay = 10)
        {
            m_RootElement = rootElement;
            m_HideDelay = hideDelay;
            
            m_MessageLabel = m_RootElement.Q<Label>("messageLabel");

            if (m_MessageLabel == null)
                throw new InvalidOperationException("MessageLabel not found in UXML.");
        }

        /// <summary>
        /// Displays a new message in this slot and starts the hiding coroutine.
        /// </summary>
        /// <param name="text">The message text to display.</param>
        public void Display(string text)
        {
            if (!isDisplaying)
            {
                isDisplaying = true;

                // Set the text of the message
                m_MessageLabel.text = text;

                // Ensure the element is visible
                m_RootElement.style.display = DisplayStyle.Flex;

                // Start the coroutine to hide after a delay
                m_RootElement.schedule.Execute(Hide).StartingIn(m_HideDelay);
            }
        }

        /// <summary>
        /// Hides the message and marks the slot as available for reuse.
        /// </summary>
        public void Hide()
        {
            if (isDisplaying)
            {
                isDisplaying = false;
                
                m_RootElement.style.display = DisplayStyle.None;
            }
        }
    }
}
