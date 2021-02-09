using UnityEngine;

namespace BossRoom.Visual
{
    /// <summary>
    /// Provides logic for a Hero Action Bar with attack, skill button and a button to open emotes panel
    /// This bar tracks button clicks on hero action buttons for later use by ClientInputSender
    /// </summary>
    public class HeroActionBar : MonoBehaviour
    {
        // All buttons in this action bar
        [SerializeField]
        private HeroActionButton[] m_Buttons;

        // The Emote panel will be enabled or disabled when clicking the last button
        [SerializeField]
        private GameObject m_EmotePanel;

        // This class will track clicks on eack of its buttons
        // ButtonWasClicked will deliver each click only once
        private bool[] m_ButtonClicked;

        // Currently this class handles setting icons for the first 3 buttons based on character type
        // Eventually this can change so it is driven by the data for each skill instead
        // Current logic will be better for demos until skills are fully implemented with icon data
        // SetPlayerType is currentlhy called to change this and trigger icon changes
        private CharacterTypeEnum m_PlayerType = CharacterTypeEnum.Tank;

        // allow icons for each class to be configured
        [SerializeField]
        private Material[] m_TankIcons;

        [SerializeField]
        private Material[] m_ArcherIcons;

        [SerializeField]
        private Material[] m_RogueIcons;

        [SerializeField]
        private Material[] m_MageIcons;

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

        public void SetPlayerType(CharacterTypeEnum playerType)
        {
            m_PlayerType = playerType;
            if (m_PlayerType == CharacterTypeEnum.Tank)
            {
                SetButtonIcons(m_TankIcons);
            }
            else if (m_PlayerType == CharacterTypeEnum.Archer)
            {
                SetButtonIcons(m_ArcherIcons);
            }
            else if (m_PlayerType == CharacterTypeEnum.Mage)
            {
                SetButtonIcons(m_MageIcons);
            }
            else if (m_PlayerType == CharacterTypeEnum.Rogue)
            {
                SetButtonIcons(m_RogueIcons);
            }
        }

        void SetButtonIcons(Material[] icons)
        {
            for (int i = 0; i < m_Buttons.Length; i++)
            {
                if (i < icons.Length)
                {
                    m_Buttons[i].image.material = icons[i];
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void onButtonClicked(int buttonIndex)
        {
            // handle emote panel as a special case
            if (buttonIndex == 3)
            {
                m_EmotePanel.SetActive(!m_EmotePanel.activeSelf);
                return;
            }
            // otherwise remember the click for input sender to grab later
            m_ButtonClicked[buttonIndex] = true;
        }

        // Return if a given button was clicked since last queried - this will also clear the value until a new click is received
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
