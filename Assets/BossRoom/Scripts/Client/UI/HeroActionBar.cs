using UnityEngine;
using UnityEngine.UI;
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
        private Button[] m_Buttons;

        // The Emote panel will be enabled or disabled when clicking the last button
        [SerializeField]
        private GameObject m_EmotePanel;

        private BossRoom.Client.ClientInputSender m_InputSender;

        // We find the Sprites to use by checking the Skill1, Skill2, and Skill3 members of our chosen CharacterClass
        private CharacterClass m_CharacterData;

        public void RegisterInputSender(Client.ClientInputSender inputSender)
        {
            if (m_InputSender != null)
            {
                Debug.LogWarning($"Multiple ClientInputSenders in scene? Discarding sender belonging to {m_InputSender.gameObject.name} and adding it for {inputSender.gameObject.name} ");
            }

            m_InputSender = inputSender;
            m_CharacterData = m_InputSender.GetComponent<NetworkCharacterState>().CharacterData;
            SetPlayerType(m_CharacterData);
        }

        private void SetPlayerType(CharacterClass characterData)
        {
            var sprites = new Sprite[]
            {
                GetSpriteForAction(characterData.Skill1),
                GetSpriteForAction(characterData.Skill2),
                GetSpriteForAction(characterData.Skill3),
            };
            SetButtonIcons(sprites);
        }

        /// <summary>
        /// Returns the Sprite for an Action, or null if no sprite is available
        /// </summary>
        private Sprite GetSpriteForAction(ActionType actionType)
        {
            if (actionType == ActionType.None)
                return null;
            var desc = GameDataSource.Instance.ActionDataByType[actionType];
            if (desc != null)
                return desc.Icon;
            return null;
        }

        void SetButtonIcons(Sprite[] icons)
        {
            for (int i = 0; i < m_Buttons.Length; i++)
            {
                if (i < icons.Length)
                {
                    m_Buttons[i].image.sprite = icons[i];
                    m_Buttons[i].gameObject.SetActive(icons[i] != null);
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
