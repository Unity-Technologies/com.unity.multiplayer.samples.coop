using System;
using BossRoom.Client;
using MLAPI;
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
        ClientInputSender m_InputSender;

        void Start()
        {
            var localPlayerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localPlayerObject &&
                localPlayerObject.TryGetComponent(out ClientInputSender clientInputSender))
            {
                RegisterInputSender(clientInputSender);
            }
            else
            {
                ClientInputSender.LocalClientReadied += RegisterInputSender;
            }

            ClientInputSender.LocalClientRemoved += DeregisterInputSender;
        }

        void RegisterInputSender(ClientInputSender inputSender)
        {
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
            ClientInputSender.LocalClientReadied -= RegisterInputSender;
            ClientInputSender.LocalClientRemoved -= DeregisterInputSender;
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
