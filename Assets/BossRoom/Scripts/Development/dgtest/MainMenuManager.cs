using MLAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private NetworkingManager m_netManager;

    // Start is called before the first frame update
    void Start()
    {
        m_netManager = GameObject.Find("NetworkHost").GetComponent<NetworkingManager>();
        if(!m_netManager)
        {
            throw new System.Exception("MainMenuManager requires the presence of a NetworkHost");
        }

        //and mark the NetworkHost as persistent. 
        GameObject.DontDestroyOnLoad(m_netManager.gameObject);
        Application.targetFrameRate = 60;
    }

    public void OnHostButton()
    {
        m_netManager.StartServer();
        ProgramState.Instance.IsHost = true;
        SceneManager.LoadScene("unitychan_test", LoadSceneMode.Single);
    }

    public void OnConnectButton()
    {

    }
}
