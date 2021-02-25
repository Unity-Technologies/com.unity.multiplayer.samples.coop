using UnityEngine;
using SkillTriggerStyle = BossRoom.Client.ClientInputSender.SkillTriggerStyle;

namespace BossRoom.Visual
{
    /// <summary>
    /// Provides logic for a Hero Emote Bar
    /// This button bar tracks button clicks and also hides after any click
    /// </summary>
    public class HeroEmoteBar : MonoBehaviour
    {
        private Client.ClientInputSender m_InputSender;

        public void RegisterInputSender(Client.ClientInputSender inputSender)
        {
            if (m_InputSender != null)
            {
                Debug.LogWarning($"Multiple ClientInputSenders in scene? Discarding sender belonging to {m_InputSender.gameObject.name} and adding it for {inputSender.gameObject.name} ");
            }

            m_InputSender = inputSender;
        }

        public void OnButtonClicked(int buttonIndex)
        {
            if( m_InputSender != null )
            {
                switch (buttonIndex)
                {
                    case 0: m_InputSender.RequestAction(ActionType.Emote1, SkillTriggerStyle.UI); break;
                    case 1: m_InputSender.RequestAction(ActionType.Emote2, SkillTriggerStyle.UI); break;
                    case 2: m_InputSender.RequestAction(ActionType.Emote3, SkillTriggerStyle.UI); break;
                    case 3: m_InputSender.RequestAction(ActionType.Emote4, SkillTriggerStyle.UI); break;
                }
            }

            // also deactivate the emote panel
            gameObject.SetActive(false);
        }
    }
}
