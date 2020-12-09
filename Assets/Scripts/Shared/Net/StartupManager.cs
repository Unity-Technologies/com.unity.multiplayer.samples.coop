using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class manages the network startup flow in the MainMenu, when the user has selected
/// either "Connect" or "Host". In the case of "Host", the flow isn't very interesting--you just start
/// up the NetworkingManager in host mode and switch to the target scene. 
/// 
/// In Connect mode, however, things are a bit more interesting. We must not only begin via the initial StartClient, but
/// also receive the first S2C RPC telling us what Scene to go to. 
/// </summary>
public class StartupManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
