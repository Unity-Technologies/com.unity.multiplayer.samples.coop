using BossRoom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BossRoom.Client
{
    /// <summary>
    /// Controls the "information box" on the character-select screen.
    /// </summary>
    /// <remarks>
    /// This box also includes the "READY" button. The Ready button's state (enabled/disabled) is controlled
    /// here, but note that the actual behavior (when clicked) is set in the editor: the button directly calls
    /// ClientCharSelectState.OnPlayerClickedReady().
    /// </remarks>
    public class UICharSelectClassInfoBox : MonoBehaviour
    {
        [SerializeField]
        private Text m_WelcomeBanner;
        [SerializeField]
        private Text m_ClassLabel;
        [SerializeField]
        private GameObject m_HideWhenNoClassSelected;
        [SerializeField]
        private Image m_ClassBanner;
        [SerializeField]
        private Image m_Skill1;
        [SerializeField]
        private Image m_Skill2;
        [SerializeField]
        private Image m_Skill3;
        [SerializeField]
        private Button m_ReadyButton;
        [SerializeField]
        [Tooltip("Message shown in the char-select screen. {0} will be replaced with the player's seat number")]
        [Multiline]
        private string m_WelcomeMsg = "Welcome, P{0}!";
        [SerializeField]
        [Tooltip("Format of tooltips. {0} is skill name, {1} is skill description. Html-esque tags allowed!")]
        [Multiline]
        private string m_TooltipFormat = "<b>{0}</b>\n\n{1}";

        private bool isLockedIn = false;

        public void OnSetPlayerNumber(int playerNumber)
        {
            m_WelcomeBanner.text = string.Format(m_WelcomeMsg, (playerNumber + 1));
        }

        public void ConfigureForNoSelection()
        {
            m_HideWhenNoClassSelected.SetActive(false);
            m_ReadyButton.interactable = false;
        }

        public void ConfigureForLockedIn()
        {
            m_ReadyButton.interactable = false;
            isLockedIn = true;
        }

        public void ConfigureForClass(CharacterTypeEnum characterType)
        {
            m_HideWhenNoClassSelected.SetActive(true);
            if (!isLockedIn)
            {
                m_ReadyButton.interactable = true;
            }

            CharacterClass characterClass = GameDataSource.Instance.CharacterDataByType[characterType];
            m_ClassLabel.text = characterClass.DisplayedName;
            m_ClassBanner.sprite = characterClass.ClassBannerLit;

            ConfigureSkillIcon(m_Skill1, characterClass.Skill1);
            ConfigureSkillIcon(m_Skill2, characterClass.Skill2);
            ConfigureSkillIcon(m_Skill3, characterClass.Skill3);
        }

        private void ConfigureSkillIcon(Image iconSlot, ActionType type)
        {
            if (type == ActionType.None)
            {
                iconSlot.gameObject.SetActive(false);
            }
            else
            {
                iconSlot.gameObject.SetActive(true);
                var data = GameDataSource.Instance.ActionDataByType[type];
                iconSlot.sprite = data.Icon;
                UITooltipDetector tooltipDetector = iconSlot.GetComponent<UITooltipDetector>();
                if (tooltipDetector)
                {
                    tooltipDetector.SetText(string.Format(m_TooltipFormat, data.DisplayedName, data.Description));
                }
            }
        }
    }
}
