using System;
using System.Collections.Generic;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Gameplay.Messages;
using Unity.BossRoom.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    // if we already have a message feed implementation why do we want to add another?
    public class UIMessageFeed : MonoBehaviour
    {
        [SerializeField]
        UIDocument uiDocument; // Reference to the main UI Document.

        [SerializeField]
        VisualTreeAsset messageItemAsset; // The UXML template for a single message item.

        VisualElement m_MessageFeedContainer; // The container for all message slots.
        List<UIMessageSlot> m_UIMessageSlots; // List of all active message slots.

        const int k_DefaultHideDelay = 10;

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

        void OnEnable()
        {
            // Get the root VisualElement
            var root = uiDocument.rootVisualElement;

            // Find the container in the UXML by name (ensure it exists in the "MessageFeed" UXML file)
            m_MessageFeedContainer = root.Q<VisualElement>("messageFeed");
            if (m_MessageFeedContainer == null)
            {
                Debug.LogError("MessageFeed container not found in UXML.");
                return;
            }

            // Initialize the message slot list
            m_UIMessageSlots = new List<UIMessageSlot>();
        }

        public void DisplayMessage(string text)
        {
            // Find an available slot or create a new one
            var messageSlot = GetAvailableSlot();
            messageSlot.Display(text);
        }

        UIMessageSlot GetAvailableSlot()
        {
            // Reuse an existing slot if one is available
            foreach (var slot in m_UIMessageSlots)
            {
                if (!slot.IsDisplaying)
                {
                    return slot;
                }
            }

            // Otherwise, create a new slot dynamically
            var newMessageElement = messageItemAsset.Instantiate(); // Clone the UXML template
            m_MessageFeedContainer.Add(newMessageElement); // Add to the container

            var newSlot = gameObject.AddComponent<UIMessageSlot>();
            m_UIMessageSlots.Add(newSlot);
            return newSlot;
        }
    }
}
