using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MessageFeed : MonoBehaviour
{
    [SerializeField]
    UIDocument doc;

    [SerializeField]
    bool AddMessage;

    List<Message> m_messages;

    VisualElement messageContainer;

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
        messageContainer.style.alignItems = Align.FlexStart; // Align messages to the start of the container
        doc.rootVisualElement.Add(messageContainer);
    }

    void Update()
    {
        if (AddMessage)
        {
            ShowMessage("Hello World");
            AddMessage = false; // Prevent repeated calls
        }

        foreach (var m in m_messages)
        {
            if (m.isShown)
            {
                // Check if a message should disappear after 10 seconds
                if (Time.realtimeSinceStartup - m.startTime > 10)
                {
                    StartFadeout(m, 1f);
                    m.isShown = false;
                }
            }
        }
    }
    
    

    void ShowMessage(string message)
    {
        // Reuse an existing message container if possible
        foreach (var m in m_messages)
        {
            if (!m.isShown)
            {
                m.isShown = true;
                m.Label.text = message;
                m.messageBox.style.display = DisplayStyle.Flex; // Show the entire message box
                m.startTime = Time.realtimeSinceStartup; // Update start time
                
                return;
            }
        }

        // If no reusable container is found, create a new one
        var newBox = new VisualElement();
        newBox.AddToClassList("messageBox"); // Apply the ".messageBox" style

        var newLabel = new Label();
        newLabel.text = message;
        newLabel.AddToClassList("message"); // Apply the ".message" style
        
        newBox.Add(newLabel); // Add the label to the box

        var newMessage = new Message()
        {
            isShown = true,
            startTime = Time.realtimeSinceStartup,
            messageBox = newBox,
            Label = newLabel
        };

        // Add the messageBox (which contains the label) to the container
        messageContainer.Add(newBox); 
        m_messages.Add(newMessage); // Track the new message
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
            }
        }).Every((long)0.1f).Until(() => opacity <= 0);
    }

    class Message
    {
        public bool isShown;          // Whether the message is currently visible
        public float startTime;       // The time when the message was shown
        public VisualElement messageBox; // The container for the message (box)
        public Label Label;           // The label inside the box
    }
}
