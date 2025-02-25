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
using Random = System.Random;

public class MessageFeed : MonoBehaviour
{
    [SerializeField]
    UIDocument doc;

    List<Message> m_Messages;

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

    [ContextMenu("Add RandomMessage")]
    public void AddRandomMessage()
    {
        var rand = new Random(nameof(AddRandomMessage).GetHashCode());
        m_Messages.Add(new Message { isShown = true, startTime = Time.realtimeSinceStartup, message = rand.Next().ToString() });
    }

    void Start()
    {
        var root = doc.rootVisualElement;

        // you could load this template from a template uxml, don't need to be a part of the visual tree
        // could make it a prop on this monobehaviour 
        // VisualTreeAsset messageTempalte; or load it via Resources api, than you don't need to hide them  
        var templateLabel = root.Q<Label>("messageLabel");
        var templateBox = root.Q<VisualElement>("messageBox");

        // Hide the default template elements
        templateLabel.style.display = DisplayStyle.None;
        templateBox.style.display = DisplayStyle.None;

        m_Messages = new List<Message>();

        // Create a container for all messages 
        var listView = root.Q<ListView>("messageList");
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


            newBox.style.opacity = 1;
            newBox.style.display = DisplayStyle.Flex;            

            newBox.RegisterCallback<AttachToPanelEvent>((e)=> StartCoroutine(FlyInWithBounce(e.target as VisualElement, -300, 50, 0.2f, 0.2f)));
            
            return newBox;
        };

        listView.bindItem += (element, i) =>
        {
            element.Q<Label>().text = m_Messages[i].message;
        };

        // collection change events will take care of creating and disposing items
        listView.itemsSource = m_Messages;
        

        m_MessageContainer = listView;
        m_MessageContainer.style.flexDirection = FlexDirection.Column; // Arrange messages vertically

        // make sure other visual elements don't get pushed down by the message container
        m_MessageContainer.style.position = Position.Absolute;
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
        foreach (var m in m_Messages)
        {
            if (m.isShown)
            {
                // Check if a message should begin fading out
                // if (Time.realtimeSinceStartup - m.startTime > 5 && m.style.opacity == 1)
                //{
                //StartFadeout(m, 1f);
                //    m.isShown = false;
                //}
            }
        }
    }

    void ShowMessage(string message)
    {
        const float messageHeight = 40f; // Approximate height of a message (adjust based on UI)
        const float verticalSpacing = 10f; // Spacing between stacked messages

        // Reuse or create a new message
        Message newMessage = null;

        foreach (var m in m_Messages)
        {
            if (!m.isShown)
            {
                // Reuse the hidden message
                newMessage = m;
                break;
            }
        }

        if (newMessage == null)
        {
            newMessage = new Message()
            {
                isShown = true,
                startTime = Time.realtimeSinceStartup,
            };

            m_Messages.Add(newMessage); // Add to the list of messages
        }

        // Set the properties of the reused or new message
        newMessage.isShown = true;

        newMessage.startTime = Time.realtimeSinceStartup;
        newMessage.message = message;
    }

    IEnumerator FlyInWithBounce(VisualElement element, float startLeft, float targetLeft, float duration, float bounceDuration)
    {
        float elapsedTime = 0;
        element.style.opacity = 0;

        // Main fly-in animation (linear movement from off-screen)
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration; // Normalized time (0 to 1)

            // Linearly interpolate left position
            float newLeft = Mathf.Lerp(startLeft, targetLeft, t);
            element.style.left = newLeft;

            // Gradually fade in
            element.style.opacity = t;

            yield return null;
        }

        // Ensure the message is at the target final position
        element.style.left = targetLeft;
        element.style.opacity = 1;

        // Bounce Animation: Overshoot to the right and come back
        float overshootAmount = 20;
        float bounceElapsedTime = 0;

        while (bounceElapsedTime < bounceDuration)
        {
            bounceElapsedTime += Time.deltaTime;
            float t = bounceElapsedTime / bounceDuration;

            // Use a simple sine easing for the bounce effect
            float bounceT = Mathf.Sin(t * Mathf.PI);

            // Interpolate between targetLeft and overshoot position
            float bounceLeft = Mathf.Lerp(targetLeft, targetLeft + overshootAmount, bounceT);

            element.style.left = bounceLeft;
            yield return null;
        }

        // Finally snap back to the exact target position
        element.style.left = targetLeft;
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
    class Message
    {
        public bool isShown;
        public float startTime; // The time when the message was shown
        public string message;
    }
}
