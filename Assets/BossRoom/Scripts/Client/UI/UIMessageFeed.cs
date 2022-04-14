using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Multiplayer.Samples.BossRoom.Visual
{
    /// <summary>
    /// Handles the display of in-game messages in a message feed
    /// </summary>
    public class UIMessageFeed : MonoBehaviour
    {
        [SerializeField]
        List<UIMessageSlot> m_MessageSlots;

        [SerializeField]
        float m_HideDelay = 5;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        GameObject m_MessageSlotPrefab;

        [SerializeField]
        VerticalLayoutGroup m_VerticalLayoutGroup;

        Coroutine m_HideFeedCoroutine;

        bool m_IsDisplaying;

        IDisposable m_Subscription;

        [Inject]
        void InjectDependencies(ISubscriber<MessageFeedHandler.MessageFeed> subscriber)
        {
        }

        void Start()
        {
            //Hide();
            StartCoroutine(test());
        }

        // Leaving this here temporarily for testing purposes
        IEnumerator test()
        {
            var i = 1;
            while (true)
            {
                yield return new WaitForSeconds(1);
                var messageSlot = GetAvailableSlot();
                messageSlot.transform.SetAsLastSibling();
                messageSlot.Display($"{i}{i}{i}{i}{i}{i}{i}{i++}");
            }
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
            var go = Instantiate(m_MessageSlotPrefab, m_VerticalLayoutGroup.transform, true);
            var messageSlot = go.GetComponentInChildren<UIMessageSlot>();
            m_MessageSlots.Add(messageSlot);
            return messageSlot;
        }

        void OnDestroy()
        {
            m_Subscription?.Dispose();
        }

    }
}
