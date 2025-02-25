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
using Random = System.Random;

public class MessageFeed : MonoBehaviour
{
    [SerializeField]
    UIDocument doc;

    List<MessageViewModel> m_Messages;

    VisualElement m_MessageContainer;

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
    

    [ContextMenu("Add Message")]
    public void AddMessage()
    {
        ShowMessage("Hello!");
    }

    void Start()
    {
        var root = doc.rootVisualElement;

        m_Messages = new List<MessageViewModel>();

        // Find the container of all messages 
        var listView = root.Q<ListView>("messageList");
        
        // Since you've added an item template in the UXML this is not really necessary here
        listView.makeItem += () =>
        {
            // Create a new message if no reusable messages are available
            var newBox = new VisualElement();
            newBox.AddToClassList("messageBox");

            //newBox.style.position = Position.Absolute; // Explicitly position it

            // Position the new message box below the last message
            //newBox.style.top = m_MessageContainer.childCount * (messageHeight + verticalSpacing);

            var newLabel = new Label();
            newLabel.AddToClassList("message");
            newBox.Add(newLabel);

            // the even when the control get's added to the "UI Canvas"
            newBox.RegisterCallback<AttachToPanelEvent>((e) =>
                (e.target as VisualElement)?.AddToClassList("messageBoxMove"));

            // fires before the element is actually removed
            newBox.RegisterCallback<DetachFromPanelEvent>((e) =>
            {
                if (e.target is VisualElement)
                {
                    
                }
            });

            return newBox;
        };

        listView.bindItem += (element, i) =>
        {
            element.Q<Label>().text = m_Messages[i].Message;
        };

        // collection change events will take care of creating and disposing items
        listView.itemsSource = m_Messages;

        // [IMPORTANT!] try to set as much of the style related logic as you can in the USS files instead.
        /*
         m_MessageContainer = listView;
         m_MessageContainer.style.flexDirection = FlexDirection.Column; // Arrange messages vertically

        // make sure other visual elements don't get pushed down by the message container
        m_MessageContainer.style.position = Position.Absolute;
        */
    }

    void OnDestroy()
    {
        if (m_Subscriptions != null)
        {
            m_Subscriptions.Dispose();
        }
    }

    void Update()
    {
        var messagesToRemove = new List<MessageViewModel>();
        foreach (var m in m_Messages)
        {
            if (m.ShouldDispose())
            {
                messagesToRemove.Add(m);
            }

            // Check if a message should begin fading out
            // if (Time.realtimeSinceStartup - m.startTime > 5 && m.style.opacity == 1)
            //{
            //StartFadeout(m, 1f);
            //    m.isShown = false;
            //}
        }

        // TODO: start animation via events
        foreach (var m in messagesToRemove)
        {
            m_Messages.Remove(m);
        }
    }

    void ShowMessage(string message)
    {
        // Reuse or create a new message
        MessageViewModel newMessage = null;

        // a list view's virtualization logic actually takes care of this by default
        /*
        foreach (var m in m_Messages)
        {
            if (!m.isShown)
            {
                // Reuse the hidden message
                newMessage = m;
                break;
            }
        }
        */
        newMessage = new MessageViewModel(message, TimeSpan.FromSeconds(5));

        m_Messages.Add(newMessage); // Add to the list of messages
    }

    /*
    static void StartFadeout(Message message, float opacity)
    {
        message.messageBox.schedule.Execute(() =>
        {
            opacity -= 0.01f;
            message.messageBox.style.opacity = opacity;

            if (opacity <= 0)
            {
                // Once faded out fully, hide the message and reset state
                message.messageBox.style.display = DisplayStyle.None;
                message.isShown = false;
                message.startTime = 0;
            }
        }).Every((long)0.1f).Until(() => opacity <= 0);
    }
    */

    // if you bind the itemsource to the list you don't actually have to manually do this
    private class MessageViewModel
    {
        private readonly TimeSpan _autoDispose;
        private DateTime _createdAt;

        public string Message { get; }

        public MessageViewModel(string message, TimeSpan timeout = default)
        {
            _createdAt = DateTime.Now;
            _autoDispose = timeout;
            Message = message;
        }

        public bool ShouldDispose()
        {
            return _createdAt + _autoDispose < DateTime.Now;
        }
    }
}
