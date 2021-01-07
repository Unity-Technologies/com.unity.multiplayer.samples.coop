using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Client
{
  /**Responsible for managing and creating a feedback icon where the player clicked to move */
  [RequireComponent(typeof(ClientInputSender))]
  public class ClickFeedback : MonoBehaviour
  {
    [SerializeField]
    GameObject m_FeedbackPrefab;
    GameObject m_FeedbackObj;
    ClientInputSender m_ClientSender;

    private const float HOVER_HEIGHT = .1f;

    // Start is called before the first frame update
    void Start()
    {
      m_ClientSender = GetComponent<ClientInputSender>();
      m_ClientSender.OnClientClick += onClick;
      m_FeedbackObj = Instantiate(m_FeedbackPrefab);
      m_FeedbackObj.SetActive(false);
    }

    void onClick(Vector3 position)
    {
      position.y += HOVER_HEIGHT;
      
      m_FeedbackObj.transform.position = position;
      m_FeedbackObj.SetActive(true);
    }

    private void OnDestroy()
    {
      m_ClientSender.OnClientClick -= onClick;
    }
  }
}

