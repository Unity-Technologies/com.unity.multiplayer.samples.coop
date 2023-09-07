using System;
using System.Collections.Generic;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Gameplay.Messages;
using Unity.BossRoom.Infrastructure;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// Handles the display of in-game messages in a message feed
    /// </summary>
    public class UIMessageFeed : MonoBehaviour
    {
        [SerializeField]
        List<UIMessageSlot> m_MessageSlots;

        [SerializeField]
        GameObject m_MessageSlotPrefab;

        [SerializeField]
        VerticalLayoutGroup m_VerticalLayoutGroup;

        DisposableGroup m_Subscriptions;

        [Inject]
        void InjectDependencies(
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ISubscriber<CheatUsedMessage> cheatUsedMessageSubscriber,
#endif
            ISubscriber<DoorStateChangedEventMessage> doorStateChangedSubscriber,
            ISubscriber<ConnectionEventMessage> connectionEventSubscriber,
            ISubscriber<LifeStateChangedEventMessage> lifeStateChangedEventSubscriber
        )
        {
            m_Subscriptions = new DisposableGroup();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            m_Subscriptions.Add(cheatUsedMessageSubscriber.Subscribe(OnCheatUsedEvent));
#endif
            m_Subscriptions.Add(doorStateChangedSubscriber.Subscribe(OnDoorStateChangedEvent));
            m_Subscriptions.Add(connectionEventSubscriber.Subscribe(OnConnectionEvent));
            m_Subscriptions.Add(lifeStateChangedEventSubscriber.Subscribe(OnLifeStateChangedEvent));
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnCheatUsedEvent(CheatUsedMessage eventMessage)
        {
            DisplayMessage($"Cheat {eventMessage.CheatUsed} used by {eventMessage.CheaterName}");
        }
#endif

        void OnDoorStateChangedEvent(DoorStateChangedEventMessage eventMessage)
        {
            DisplayMessage(eventMessage.IsDoorOpen ? "The Door has been opened!" : "The Door is closing.");
        }

        void OnConnectionEvent(ConnectionEventMessage eventMessage)
        {
            switch (eventMessage.ConnectStatus)
            {
                case ConnectStatus.Success:
                    DisplayMessage($"{eventMessage.PlayerName} has joined the game!");
                    break;
                case ConnectStatus.ServerFull:
                case ConnectStatus.LoggedInAgain:
                case ConnectStatus.UserRequestedDisconnect:
                case ConnectStatus.GenericDisconnect:
                case ConnectStatus.IncompatibleBuildType:
                case ConnectStatus.HostEndedSession:
                    DisplayMessage($"{eventMessage.PlayerName} has left the game!");
                    break;
            }
        }

        void OnLifeStateChangedEvent(LifeStateChangedEventMessage eventMessage)
        {
            switch (eventMessage.CharacterType)
            {
                case CharacterTypeEnum.Tank:
                case CharacterTypeEnum.Archer:
                case CharacterTypeEnum.Mage:
                case CharacterTypeEnum.Rogue:
                case CharacterTypeEnum.ImpBoss:
                    switch (eventMessage.NewLifeState)
                    {
                        case LifeState.Alive:
                            DisplayMessage($"{eventMessage.CharacterName} has been reanimated!");
                            break;
                        case LifeState.Fainted:
                        case LifeState.Dead:
                            DisplayMessage($"{eventMessage.CharacterName} has been defeated!");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
            }
        }

        void DisplayMessage(string text)
        {
            var messageSlot = GetAvailableSlot();
            messageSlot.Display(text);
        }

        UIMessageSlot GetAvailableSlot()
        {
            foreach (var slot in m_MessageSlots)
            {
                if (!slot.IsDisplaying)
                {
                    return slot;
                }
            }
            var go = Instantiate(m_MessageSlotPrefab, m_VerticalLayoutGroup.transform);
            var messageSlot = go.GetComponentInChildren<UIMessageSlot>();
            m_MessageSlots.Add(messageSlot);
            return messageSlot;
        }

        void OnDestroy()
        {
            if (m_Subscriptions != null)
            {
                m_Subscriptions.Dispose();
            }
        }

    }
}
