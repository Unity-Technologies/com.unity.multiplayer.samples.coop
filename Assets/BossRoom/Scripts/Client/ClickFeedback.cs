using MLAPI;
using UnityEngine;

namespace BossRoom.Client
{
    /// <summary>
    /// Responsible for managing and creating a feedback icon where the player clicked to move
    /// </summary>
    [RequireComponent(typeof(ClientInputSender))]
    public class ClickFeedback : NetworkBehaviour
    {
        [SerializeField]
        BossRoomPlayerCharacter m_BossRoomPlayerCharacter;

        [SerializeField]
        ClientInputSender m_ClientInputSender;

        [SerializeField]
        GameObject m_FeedbackPrefab;

        GameObject m_FeedbackObj;

        const float k_HoverHeight = 0.15f;

        public override void NetworkStart()
        {
            if (!IsClient)
            {
                Destroy(this);
            }
            else
            {
                if (m_BossRoomPlayerCharacter)
                {
                    if (m_BossRoomPlayerCharacter.BossRoomPlayer)
                    {
                        NetworkInitialize();
                    }
                    else
                    {
                        m_BossRoomPlayerCharacter.BossRoomPlayerNetworkReadied += NetworkInitialize;
                        enabled = false;
                    }
                }
            }
        }

        void NetworkInitialize()
        {
            if (!m_BossRoomPlayerCharacter.BossRoomPlayer.IsLocalPlayer)
            {
                Destroy(this);
                return;
            }

            m_ClientInputSender.ClientMoveEvent += OnClientMove;
            m_FeedbackObj = Instantiate(m_FeedbackPrefab);
            m_FeedbackObj.SetActive(false);

            enabled = true;
        }

        void OnClientMove(Vector3 position)
        {
            position.y += k_HoverHeight;

            m_FeedbackObj.transform.position = position;
            m_FeedbackObj.SetActive(true);
        }

        void OnDestroy()
        {
            if (m_BossRoomPlayerCharacter)
            {
                m_BossRoomPlayerCharacter.BossRoomPlayerNetworkReadied -= NetworkInitialize;
            }
            if (m_ClientInputSender)
            {
                m_ClientInputSender.ClientMoveEvent -= OnClientMove;
            }
        }
    }
}
