using MLAPI;
using UnityEngine;

namespace BossRoom.Client
{
    /// <summary>
    /// Responsible for managing and creating a feedback icon where the player clicked to move
    /// </summary>
    [RequireComponent(typeof(ClientInputSender))]
    public class ClickFeedback : MonoBehaviour
    {
        [SerializeField]
        GameObject m_FeedbackPrefab;
        GameObject m_FeedbackObj;
        ClientInputSender m_ClientSender;

        const float k_HoverHeight = .1f;

        void Start()
        {
            var networkedObject = GetComponent<NetworkObject>();
            if (networkedObject == null || !networkedObject.IsLocalPlayer)
            {
                Destroy(this);
                return;
            }

            m_ClientSender = GetComponent<ClientInputSender>();
            m_ClientSender.ClientMoveRequested += ShowMoveFeedback;
            m_FeedbackObj = Instantiate(m_FeedbackPrefab);
            m_FeedbackObj.SetActive(false);
        }

        void ShowMoveFeedback(Vector3 position)
        {
            position.y += k_HoverHeight;

            m_FeedbackObj.transform.position = position;
            m_FeedbackObj.SetActive(true);
        }

        void OnDestroy()
        {
            if (m_ClientSender)
            {
                m_ClientSender.ClientMoveRequested -= ShowMoveFeedback;
            }
        }
    }
}
