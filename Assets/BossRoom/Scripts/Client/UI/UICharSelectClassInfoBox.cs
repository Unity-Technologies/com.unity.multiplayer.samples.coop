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
        Text m_WelcomeBanner;
        [SerializeField]
        Text m_ClassLabel;
        [SerializeField]
        GameObject m_HideWhenNoClassSelected;
        [SerializeField]
        Image m_ClassBanner;
        [SerializeField]
        Image m_Skill1;
        [SerializeField]
        Image m_Skill2;
        [SerializeField]
        Image m_Skill3;
        [SerializeField]
        Button m_ReadyButton;
        [SerializeField]
        [Tooltip("Message shown in the char-select screen. {0} will be replaced with the player's seat number")]
        [Multiline]
        string m_WelcomeMsg = "Welcome, P{0}!";
        [SerializeField]
        [Tooltip("Format of tooltips. {0} is skill name, {1} is skill description. Html-esque tags allowed!")]
        [Multiline]
        string m_TooltipFormat = "<b>{0}</b>\n\n{1}";

        bool m_IsLockedIn;

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
            m_IsLockedIn = true;
        }

        public void ConfigureForClass(CharacterTypeEnum characterType)
        {
            m_HideWhenNoClassSelected.SetActive(true);
            if (!m_IsLockedIn)
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

        void ConfigureSkillIcon(Image iconSlot, ActionType type)
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
