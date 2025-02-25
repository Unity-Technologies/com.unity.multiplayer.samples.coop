using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Gameplay.Messages;
using Unity.BossRoom.Infrastructure;
using VContainer;

public class MessageFeed : MonoBehaviour
{
    [SerializeField]
    UIDocument doc;

    List<MessageViewModel> m_Messages;
    List<MessageViewModel> _messagesToRemove = new List<MessageViewModel>();

    ListView m_MessageContainer;

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
        ShowMessage($"Hello! {DateTime.Now.Millisecond}");
    }

    //would be much nicer if this would be a custom control, and we'd do this in an attach to panel event
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

            var newLabel = new Label();
            newLabel.AddToClassList("message");
            newBox.Add(newLabel);

            // the event when the control get's added to the "UI Canvas"
            newBox.RegisterCallback<AttachToPanelEvent>((e) =>
            {
                if (e.target is VisualElement element)
                {
                    element.RemoveFromClassList("messageBoxMove");
                    StartCoroutine(ToggleClassWithDelay(element, "messageBoxMove", TimeSpan.FromSeconds(0.02)));
                }
            });

            // fires just before the element is actually removed
            newBox.RegisterCallback<DetachFromPanelEvent>((e) =>
            {
                if (e.target is VisualElement) { }
            });

            return newBox;
        };
        
        
        listView.destroyItem += (element) =>
        {
            
        };

        // use this to set bindings / values on your view components
        listView.bindItem += (element, i) =>
        {
            var label = element.Q<Label>();
            label.text = m_Messages[i].Message; 
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

        m_MessageContainer = listView;
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
        if (m_Messages == null)
            return;

        foreach (var m in m_Messages)
        {
            if (m.ShouldDispose() && !_messagesToRemove.Contains(m))
            {
                _messagesToRemove.Add(m);
            }
        }

        foreach (var m in _messagesToRemove)
        {
            var fadeOutClassName = "messageBoxFadeOut";

            var child = m_MessageContainer.Query<VisualElement>().Class("messageBox")
                .AtIndex(m_Messages.IndexOf(m));
            
            if (!child.ClassListContains(fadeOutClassName))
            {
                child.AddToClassList(fadeOutClassName);
                child.RegisterCallback<TransitionEndEvent>((e) =>
                {
                    m_Messages.Remove(m);
                    _messagesToRemove.Remove(m);
                    child.RemoveFromClassList(fadeOutClassName);
                });
            }
        }
    }

    void ShowMessage(string message)
    {
        var newMessage = new MessageViewModel(message, TimeSpan.FromSeconds(5));

        m_Messages.Add(newMessage); // Add to the list of messages
    }

    IEnumerator ToggleClassWithDelay(VisualElement element, string className, TimeSpan delay)
    {
        yield return new WaitForSeconds((float)delay.TotalSeconds);

        element.ToggleInClassList(className);
    }


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

        // probably an event would be nicer
        public bool ShouldDispose()
        {
            return _createdAt + _autoDispose < DateTime.Now;
        }
    }
}
