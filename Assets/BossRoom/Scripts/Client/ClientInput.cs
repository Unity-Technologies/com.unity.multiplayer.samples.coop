using MLAPI;
using MLAPI.NetworkedVar;
using UnityEngine;

[RequireComponent(typeof(NetworkCharacterState))]
public class ClientInput : NetworkedBehaviour
{
    private NetworkCharacterState networkCharacter;

    public override void NetworkStart()
    {
        // TODO The entire disabling/enabling is still sketchy and the reason why this has to be a NetworkedBehaviour
        if (!IsClient)
        {
            enabled = false;
        }
    }


    void Awake()
    {
        networkCharacter = GetComponent<NetworkCharacterState>();
        enabled = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // EDU Multiplayer games poll update in fixed step because server processes game simulation in a fixed step as well

        // TODO can we use new Unity input system which supports fixed update polling? Right now implementation is broken

        // Is mouse button pressed (not checking for down for continuous input)
        if (Input.GetMouseButton(0))
        {
            RaycastHit hit;
            
            // TODO Camera.main is horrible in Unity < 2020.2
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
            {
                // TODO Send reliable sequenced
                // TODO Call syntax is still ugly
                networkCharacter.InvokeServerRpc(networkCharacter.ServerRpcReceiveMovementInput, hit.point);
            }
        }
    }
}
