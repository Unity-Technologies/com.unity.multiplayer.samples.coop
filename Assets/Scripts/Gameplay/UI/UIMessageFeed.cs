using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Gameplay.Messages;
using Unity.BossRoom.Infrastructure;
using VContainer;

namespace Unity.BossRoom.Gameplay.UI
{
    public class UIMessageFeed : MonoBehaviour
    {
        const string k_MessageBoxMovementClassName = "messageBoxMove";
        const string k_FadeOutClassName = "messageBoxFadeOut";
        const string k_MessageBoxClassName = "messageBox";
        const string k_MessageClassName = "message";

        [SerializeField]
        UIDocument doc;

        ObservableCollection<MessageViewModel> m_Messages;
        List<MessageViewModel> m_MessagesToRemove = new List<MessageViewModel>();

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
            ShowMessage($"Hello! {DateTime.Now.Millisecond}");
        }

        void Start()
        {
            var root = doc.rootVisualElement;

            m_Messages = new ObservableCollection<MessageViewModel>();

            // Find the container of all messages 
            m_MessageContainer = root.Q<VisualElement>("messageFeed");

            // This will handle the addition and removal of the message presenters
            m_Messages.CollectionChanged += OnMessageCollectionChanged;
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
                if (m.ShouldDispose() && !m_MessagesToRemove.Contains(m))
                {
                    m_MessagesToRemove.Add(m);
                }
            }

            foreach (var message in m_MessagesToRemove)
            {
                var childQuery = m_MessageContainer.Query<VisualElement>().Class(k_MessageBoxClassName);
                var child = childQuery.Where(a => a.Q<Label>().text == message.Message).First();

                if (!child.ClassListContains(k_FadeOutClassName))
                {
                    child.AddToClassList(k_FadeOutClassName);
                    child.RegisterCallback<TransitionEndEvent>(OnTransitionEndEvent);
                }

                // local event handler function
                void OnTransitionEndEvent(TransitionEndEvent e)
                {
                    if (e.target is VisualElement element)
                    {
                        m_Messages.Remove(message);
                        m_MessagesToRemove.Remove(message);
                    }
                }
            }
        }

        void ShowMessage(string message)
        {
            var newMessage = new MessageViewModel(message, TimeSpan.FromSeconds(5));

            m_Messages.Add(newMessage); // Add to the list of messages
        }

        void OnMessageCollectionChanged(object sender, NotifyCollectionChangedEventArgs eventArgs)
        {
            switch (eventArgs.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    OnMessagesAdded(eventArgs);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    OnMessagesRemoved(eventArgs);
                    break;
                default:
                    Debug.LogWarning("Collection was modified in an unexpected way");
                    break;
            }
        }

        void OnMessagesRemoved(NotifyCollectionChangedEventArgs eventArgs)
        {
            foreach (var itemToRemove in eventArgs.OldItems)
            {
                if (itemToRemove is MessageViewModel messageViewModel)
                {
                    var childQuery = m_MessageContainer.Query<VisualElement>().Class(k_MessageBoxClassName);
                    var child = childQuery.Where(a => a.Q<Label>().text == messageViewModel.Message).First();

                    // manually removing the child item from the message feed
                    m_MessageContainer.contentContainer.Remove(child);
                }
            }
        }

        void OnMessagesAdded(NotifyCollectionChangedEventArgs eventArgs)
        {
            foreach (var message in eventArgs.NewItems)
            {
                if (message is not MessageViewModel messageViewModel)
                    return;

                // Create a new messageBox
                var messageBox = new VisualElement();
                messageBox.AddToClassList(k_MessageBoxClassName);

                var messagePresenter = new Label();
                messagePresenter.AddToClassList(k_MessageClassName);
                messagePresenter.text = messageViewModel.Message;

                // Add the message presenter into the box
                messageBox.Add(messagePresenter);

                // the event when the control get's added to the "UI Canvas"
                messageBox.RegisterCallback<AttachToPanelEvent>(OnAttachToPanelEvent);

                // Add the message box to the message Feed
                m_MessageContainer.contentContainer.Add(messageBox);

                return;

                void OnAttachToPanelEvent(AttachToPanelEvent evt)
                {
                    if (evt.target is VisualElement element)
                    {
                        // we set up the control in a way that it starts with an offset.
                        // we schedule the transition for the message to snap in back to it's intended position
                        element.schedule
                            .Execute(() => element.ToggleInClassList(k_MessageBoxMovementClassName))
                            .ExecuteLater(200);
                    }
                }
            }
        }

        // if you bind the itemsource to the list you don't actually have to manually do this
        class MessageViewModel
        {
            readonly TimeSpan _autoDispose;
            DateTime _createdAt;

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
}
