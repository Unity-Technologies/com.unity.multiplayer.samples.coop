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
    GameObject feedbackPrefab;
    private GameObject feedbackObj;
    private ClientInputSender m_ClientSender;
    private float lastClicked;

    // Start is called before the first frame update
    void Start()
    {
      m_ClientSender = GetComponent<ClientInputSender>();
      m_ClientSender.OnClientClick += onClick;
      lastClicked = Time.time;
    }

    void onClick(Vector3 position)
    {

      if (!feedbackObj)
      {
        feedbackObj = Instantiate(feedbackPrefab);
      }
      position.y += .1f;
      
      feedbackObj.transform.position = position;
      feedbackObj.SetActive(true);

      lastClicked = Time.time;
    }
    

    // Update is called once per frame
    void Update()
    {
      if (feedbackObj && Time.time - lastClicked >= .75)
      {
        Destroy(feedbackObj);
        feedbackObj = null;
      }
    }

    private void OnDestroy()
    {
      m_ClientSender.OnClientClick -= onClick;
    }
  }
}

