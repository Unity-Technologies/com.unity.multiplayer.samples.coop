using System;
using NUnit.Framework;
using Unity.BossRoom.Infrastructure;
using UnityEngine;

namespace Unity.BossRoom.Tests.Runtime
{
    public class MessageChannelTests
    {
        struct EmptyMessage { }

        int m_NbSubscribers = 2;
        int m_NbMessagesReceived;

        IDisposable SubscribeToChannel(MessageChannel<EmptyMessage> channel)
        {
            var subscriptions = new DisposableGroup();
            subscriptions.Add(channel.Subscribe(Subscription1));
            subscriptions.Add(channel.Subscribe(Subscription2));

            return subscriptions;
        }

        void Subscription1(EmptyMessage message)
        {
            m_NbMessagesReceived++;
        }

        void Subscription2(EmptyMessage message)
        {
            m_NbMessagesReceived++;
        }


        [SetUp]
        public void Setup()
        {
            m_NbMessagesReceived = 0;
        }

        [Test]
        public void MessagePublishedIsReceivedByAllSubscribers()
        {
            var messageChannel = new MessageChannel<EmptyMessage>();
            var subscriptions = SubscribeToChannel(messageChannel);

            messageChannel.Publish(new EmptyMessage());
            Assert.AreEqual(m_NbSubscribers, m_NbMessagesReceived);
            subscriptions.Dispose();
        }

        [Test]
        public void MessagePublishedIsNotReceivedByAllSubscribersAfterUnsubscribing()
        {
            var messageChannel = new MessageChannel<EmptyMessage>();
            var subscriptions = SubscribeToChannel(messageChannel);

            messageChannel.Publish(new EmptyMessage());
            Assert.AreEqual(m_NbSubscribers, m_NbMessagesReceived);

            m_NbMessagesReceived = 0;

            subscriptions.Dispose();

            messageChannel.Publish(new EmptyMessage());
            Assert.AreEqual(0, m_NbMessagesReceived);
        }

        [Test]
        public void MessagePublishedIsReceivedByAllSubscribersAfterResubscribing()
        {
            var messageChannel = new MessageChannel<EmptyMessage>();
            var subscriptions = SubscribeToChannel(messageChannel);

            messageChannel.Publish(new EmptyMessage());
            Assert.AreEqual(m_NbSubscribers, m_NbMessagesReceived);

            m_NbMessagesReceived = 0;

            subscriptions.Dispose();
            subscriptions = SubscribeToChannel(messageChannel);

            messageChannel.Publish(new EmptyMessage());
            Assert.AreEqual(m_NbSubscribers, m_NbMessagesReceived);
            subscriptions.Dispose();
        }
    }
}
