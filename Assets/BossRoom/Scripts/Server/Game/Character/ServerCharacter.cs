using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ServerCharacter : MLAPI.NetworkedBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public override void NetworkStart()
    {
        if( !IsServer ) { this.enabled = false; }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
