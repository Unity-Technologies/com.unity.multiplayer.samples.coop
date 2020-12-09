using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public GameObject NetHostGO;

    private MLAPI.NetworkingManager m_netManager;

    // Start is called before the first frame update
    void Start()
    {
        m_netManager = NetHostGO.GetComponent<MLAPI.NetworkingManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnHostClicked()
    {
        Debug.Log("Host Clicked");

        //TODO: bring up transition screen. 

        m_netManager.StartHost();
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void OnConnectClicked()
    {
        Debug.Log("Connect Clicked");

    }
}
