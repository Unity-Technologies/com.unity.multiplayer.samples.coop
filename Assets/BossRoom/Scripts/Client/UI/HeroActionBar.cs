using UnityEngine;
using SkillTriggerStyle = BossRoom.Client.ClientInputSender.SkillTriggerStyle;

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

        private BossRoom.Client.ClientInputSender m_InputSender;

        // Currently we manually configure icons from the Material arrays stored on this class, and we select which array to use
        //from the CharacterClass inferred from the registered player GameObject. Eventually this can change so it is driven by
        //the data for each skill instead. Current logic will be better for demos until skills are fully implemented with icon data. 
        private CharacterClass m_CharacterData;

        // allow icons for each class to be configured
        [SerializeField]
        private Sprite[] m_TankIcons;

        [SerializeField]
        private Sprite[] m_ArcherIcons;

        [SerializeField]
        private Sprite[] m_RogueIcons;

        [SerializeField]
        private Sprite[] m_MageIcons;

        public void RegisterInputSender(Client.ClientInputSender inputSender)
        {
            if (m_InputSender != null)
            {
                Debug.LogWarning($"Multiple ClientInputSenders in scene? Discarding sender belonging to {m_InputSender.gameObject.name} and adding it for {inputSender.gameObject.name} ");
            }

            m_InputSender = inputSender;
            m_CharacterData = m_InputSender.GetComponent<NetworkCharacterState>().CharacterData;
            SetPlayerType(m_CharacterData.CharacterType);
        }

        private void SetPlayerType(CharacterTypeEnum playerType)
        {
            if (playerType == CharacterTypeEnum.Tank)
            {
                SetButtonIcons(m_TankIcons);
            }
            else if (playerType == CharacterTypeEnum.Archer)
            {
                SetButtonIcons(m_ArcherIcons);
            }
            else if (playerType == CharacterTypeEnum.Mage)
            {
                SetButtonIcons(m_MageIcons);
            }
            else if (playerType == CharacterTypeEnum.Rogue)
            {
                SetButtonIcons(m_RogueIcons);
            }
        }

        void SetButtonIcons(Sprite[] icons)
        {
            for (int i = 0; i < m_Buttons.Length; i++)
            {
                if (i < icons.Length)
                {
                    m_Buttons[i].image.sprite = icons[i];
                }
            }
        }

        public void OnButtonClicked(int buttonIndex)
        {
            if (buttonIndex == 3)
            {
                m_EmotePanel.SetActive(!m_EmotePanel.activeSelf);
                return;
            }

            if (m_InputSender == null)
            {
                //nothing to do past this point if we don't have an InputSender.
                return;
            }

            switch (buttonIndex)
            {
                case 0: m_InputSender.RequestAction(m_CharacterData.Skill1, SkillTriggerStyle.UI); break;
                case 1: m_InputSender.RequestAction(m_CharacterData.Skill2, SkillTriggerStyle.UI); break;
                case 2: m_InputSender.RequestAction(m_CharacterData.Skill3, SkillTriggerStyle.UI); break;
            }
        }
    }
}
