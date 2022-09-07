using System;
using Unity.BossRoom.Gameplay.UserInput;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// Responsible for managing and creating a feedback icon where the player clicked to move
    /// </summary>
    [RequireComponent(typeof(ClientInputSender))]
    public class ClientClickFeedback : NetworkBehaviour
    {
        [SerializeField]
        GameObject m_FeedbackPrefab;

        GameObject m_FeedbackObj;

        ClientInputSender m_ClientSender;

        ClickFeedbackLerper m_ClickFeedbackLerper;


        void Start()
        {
            if (NetworkManager.Singleton.LocalClientId != OwnerClientId)
            {
                enabled = false;
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

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (m_ClientSender)
            {
                m_ClientSender.ClientMoveEvent -= OnClientMove;
            }

        }
    }
}
