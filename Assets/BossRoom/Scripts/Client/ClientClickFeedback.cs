using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Client
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

        const float k_HoverHeight = 0.15f;

        void Start()
        {
            if (NetworkManager.Singleton.LocalClientId != OwnerClientId)
            {
                Destroy(this);
                return;
            }

            m_ClientSender = GetComponent<ClientInputSender>();
            m_ClientSender.ClientMoveEvent += OnClientMove;
            m_FeedbackObj = Instantiate(m_FeedbackPrefab);
            m_FeedbackObj.SetActive(false);
        }

        void OnClientMove(Vector3 position)
        {
            position.y += k_HoverHeight;

            m_FeedbackObj.transform.position = position;
            m_FeedbackObj.SetActive(true);

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
