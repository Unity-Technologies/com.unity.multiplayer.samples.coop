using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Gameplay.Messages;
using Unity.BossRoom.Infrastructure;
using VContainer;
using System.Linq;

public class MessageFeed : MonoBehaviour
{
    [SerializeField]
    UIDocument doc;

    List<Message> m_messages;

    VisualElement messageContainer;

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
        ShowMessage($"Cheat {eventMessage.CheatUsed} used by {eventMessage.CheaterName}");
    }
#endif

    void OnDoorStateChangedEvent(DoorStateChangedEventMessage eventMessage)
    {
        ShowMessage(eventMessage.IsDoorOpen ? "The Door has been opened!" : "The Door is closing.");
    }

    void OnConnectionEvent(ConnectionEventMessage eventMessage)
    {
        switch (eventMessage.ConnectStatus)
        {
            case ConnectStatus.Success:
                ShowMessage($"{eventMessage.PlayerName} has joined the game!");
                break;
            case ConnectStatus.ServerFull:
            case ConnectStatus.LoggedInAgain:
            case ConnectStatus.UserRequestedDisconnect:
            case ConnectStatus.GenericDisconnect:
            case ConnectStatus.IncompatibleBuildType:
            case ConnectStatus.HostEndedSession:
                ShowMessage($"{eventMessage.PlayerName} has left the game!");
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
                        ShowMessage($"{eventMessage.CharacterName} has been reanimated!");
                        break;
                    case LifeState.Fainted:
                    case LifeState.Dead:
                        ShowMessage($"{eventMessage.CharacterName} has been defeated!");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                break;
        }
    }

    void Start()
    {
        var root = doc.rootVisualElement;
        var templateLabel = root.Q<Label>("messageLabel");
        var templateBox = root.Q<VisualElement>("messageBox");

        // Hide the default template elements
        templateLabel.style.display = DisplayStyle.None;
        templateBox.style.display = DisplayStyle.None;

        m_messages = new List<Message>();

        // Create a container for all messages
        messageContainer = new VisualElement();
        messageContainer.style.flexDirection = FlexDirection.Column; // Arrange messages vertically

        // make sure other visual elements don't get pushed down by the message container
        messageContainer.style.position = Position.Absolute;
        doc.rootVisualElement.Add(messageContainer);
    }

    void OnDestroy()
    {
        if (m_Subscriptions != null)
        {
            m_Subscriptions.Dispose();
        }
    }

    static void StartFadeout(Message message, float opacity)
    {
        message.messageBox.style.opacity = opacity;
        message.messageBox.schedule.Execute(() =>
        {
            opacity -= 0.01f;
            message.messageBox.style.opacity = opacity;
            if (opacity <= 0)
            {
                message.messageBox.style.display = DisplayStyle.None;
                message.startTime = 0;
            }
        }).Every((long)0.1f).Until(() => opacity <= 0);
    }

    void ShowMessage(string message)
    {
        // Limit maximum number of active messages
        int maxMessages = 10;
        if (m_messages.Count(m => m.isShown) >= maxMessages)
        {
            // Find the oldest active message and start fading it out
            var oldestMessage = m_messages.FirstOrDefault(m => m.isShown && m.startTime > 0);
            if (oldestMessage != null)
            {
                StartFadeout(oldestMessage, 1f);
                oldestMessage.isShown = false;
            }
        }

        // Reuse or create a new message
        foreach (var m in m_messages)
        {
            if (!m.isShown)
            {
                m.isShown = true;
                m.Label.text = message;
                m.messageBox.style.display = DisplayStyle.Flex;
                m.startTime = Time.realtimeSinceStartup;

                return;
            }
        }

        // Create a new message container
        var newBox = new VisualElement();
        newBox.AddToClassList("messageBox");

        var newLabel = new Label();
        newLabel.text = message;
        newLabel.AddToClassList("message");

        newBox.Add(newLabel);

        var newMessage = new Message()
        {
            isShown = true,
            startTime = Time.realtimeSinceStartup,
            messageBox = newBox,
            Label = newLabel
        };

        messageContainer.Add(newBox);
        m_messages.Add(newMessage);
    }
    

    class Message
    {
        public bool isShown;
        public float startTime; // The time when the message was shown
        public VisualElement messageBox;
        public Label Label;
    }
}
