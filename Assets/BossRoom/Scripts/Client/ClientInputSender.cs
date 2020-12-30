using MLAPI;
using UnityEngine;

namespace BossRoom.Client
{
    /// <summary>
    /// Captures inputs for a character on a client and sends them to the server.
    /// </summary>
    [RequireComponent(typeof(NetworkCharacterState))]
    public class ClientInputSender : NetworkedBehaviour
    {
        private NetworkCharacterState networkCharacter;

        public override void NetworkStart()
        {
            // TODO Don't use NetworkedBehaviour for just NetworkStart [GOMPS-81]
            if (!IsClient || !IsOwner)
            {
                enabled = false;
            }
        }


        void Awake()
        {
            networkCharacter = GetComponent<NetworkCharacterState>();
        }

        void FixedUpdate()
        {
            // TODO replace with new Unity Input System [GOMPS-81]

            // Is mouse button pressed (not just checking for down to allow continuous movement inputs by holding the mouse button down)
            if (Input.GetMouseButton(0))
            {
                RaycastHit hit;
                
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                {
                    // The MLAPI_INTERNAL channel is a reliable sequenced channel. Inputs should always arrive and be in order that's why this channel is used.
                    networkCharacter.InvokeServerRpc(networkCharacter.SendCharacterInputServerRpc, hit.point, "MLAPI_INTERNAL");
                }
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                var data = new ActionRequestData();
                data.ActionTypeEnum = Action.TANK_BASEATTACK;
                networkCharacter.C2S_DoAction(ref data);
            }
        }
    }
}
