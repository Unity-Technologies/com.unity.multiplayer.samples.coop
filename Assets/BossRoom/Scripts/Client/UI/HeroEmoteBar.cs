using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// Provides logic for a Hero Emote Bar
    /// This button bar tracks button clicks and also hides after any click
    /// </summary>
    public class HeroEmoteBar : MonoBehaviour
    {
        // All buttons in this UI button bar
        [SerializeField]
        private HeroActionButton[] m_Buttons;

        // This class will track clicks on eack of its buttons
        // ButtonWasClicked will deliver each click only once
        private bool[] m_ButtonClicked;

        // Start is called before the first frame update
        void Start()
        {
            // clear clicked state
            m_ButtonClicked = new bool[m_Buttons.Length];
            for (int i = 0; i < m_Buttons.Length; i++)
            {
                // initialize all button states to not clicked
                m_ButtonClicked[i] = false;
            }
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void onButtonClicked(int buttonIndex)
        {
            m_ButtonClicked[buttonIndex] = true;

            // also deactivate the emote panel
            gameObject.SetActive(false);
        }

        // return if a button was clicked since last queried - this will also clear the value until a new click is received
        public bool ButtonWasClicked(int buttonIndex)
        {
            // if we are not started yet or index is above our array lengths just rethr false
            if (m_ButtonClicked == null || buttonIndex >= m_Buttons.Length || buttonIndex >= m_ButtonClicked.Length)
            {
                return false;
            }
            bool wasClicked = m_ButtonClicked[buttonIndex];
            // set to false so we only trigger once per button
            m_ButtonClicked[buttonIndex] = false;
            return wasClicked;
        }
    }
}
