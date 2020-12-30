using MLAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ProgramState is a persistent class and GameObject that lives across all scenes. It is used to pass state across scene transition boundaries. 
/// </summary>
public class ProgramState : MonoBehaviour
{
    public bool IsHost = false;

    //NetworkingManager is contained on the NetworkHost, which is also a persistent GameObject.
    private NetworkingManager m_netManager;

    public static ProgramState Instance;

    public NetworkingManager NetManager { get { return m_netManager;  } }

    /// <summary>
    /// The synchronized network time (in seconds, since the server started). 
    /// </summary>
    public float NetTime {  get { return m_netManager.NetworkTime; } }

    // Start is called before the first frame update
    void Start()
    {
        GameObject.DontDestroyOnLoad(this.gameObject);
        Instance = this;

        m_netManager = GameObject.Find("NetworkHost").GetComponent<NetworkingManager>();
    }

}
