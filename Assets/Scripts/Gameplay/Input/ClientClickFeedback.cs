using System;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Responsible for managing and creating a feedback icon where the player clicked to move
    /// </summary>
    [RequireComponent(typeof(ClientInputSender))]
    [RequireComponent(typeof(NetcodeHooks))]
    public class ClientClickFeedback : MonoBehaviour
    {
        [SerializeField]
        GameObject m_FeedbackPrefab;

        GameObject m_FeedbackObj;

        ClientInputSender m_ClientSender;

        ClickFeedbackLerper m_ClickFeedbackLerper;
        NetcodeHooks m_NetcodeHooks;

        void Awake()
        {
            m_NetcodeHooks = GetComponent<NetcodeHooks>();
        }

        void Start()
        {
            if (NetworkManager.Singleton.LocalClientId != m_NetcodeHooks.OwnerClientId)
            {
                Destroy(this);
                return;
            }

            m_ClientSender = GetComponent<ClientInputSender>();
            m_ClientSender.ClientMoveEvent += OnClientMove;
            m_FeedbackObj = Instantiate(m_FeedbackPrefab);
            m_FeedbackObj.SetActive(false);
            m_ClickFeedbackLerper = m_FeedbackObj.GetComponent<ClickFeedbackLerper>();
        }

        void OnClientMove(Vector3 position)
        {
            m_FeedbackObj.SetActive(true);
            m_ClickFeedbackLerper.SetTarget(position);
        }

        public void OnDestroy()
        {
            if (m_ClientSender)
            {
                m_ClientSender.ClientMoveEvent -= OnClientMove;
            }
        }
    }
}
