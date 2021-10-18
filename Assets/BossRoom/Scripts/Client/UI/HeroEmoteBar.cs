using Unity.Multiplayer.Samples.BossRoom.Client;
using UnityEngine;
using SkillTriggerStyle = Unity.Multiplayer.Samples.BossRoom.Client.ClientInputSender.SkillTriggerStyle;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// Provides logic for a Hero Emote Bar
    /// This button bar tracks button clicks and also hides after any click
    /// </summary>
    public class HeroEmoteBar : MonoBehaviour
    {
        ClientInputSender m_InputSender;

        void Awake()
        {
            ClientPlayerAvatar.LocalClientSpawned += RegisterInputSender;
            ClientPlayerAvatar.LocalClientDespawned += DeregisterInputSender;
        }

        void RegisterInputSender(ClientPlayerAvatar clientPlayerAvatar)
        {
            if (!clientPlayerAvatar.TryGetComponent(out ClientInputSender inputSender))
            {
                Debug.LogError("ClientInputSender not found on ClientPlayerAvatar!", clientPlayerAvatar);
            }

            if (m_InputSender != null)
            {
                Debug.LogWarning($"Multiple ClientInputSenders in scene? Discarding sender belonging to {m_InputSender.gameObject.name} and adding it for {inputSender.gameObject.name} ");
            }

            m_InputSender = inputSender;

            gameObject.SetActive(false);
        }

        void DeregisterInputSender()
        {
            m_InputSender = null;
        }

        void OnDestroy()
        {
            ClientPlayerAvatar.LocalClientSpawned -= RegisterInputSender;
            ClientPlayerAvatar.LocalClientDespawned -= DeregisterInputSender;
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
