using BossRoom.Shared;
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
        private NetworkCharacterState m_NetworkCharacter;

        public override void NetworkStart()
        {
            // TODO [GOMPS-81] Don't use NetworkedBehaviour for just NetworkStart
            if (!IsClient || !IsOwner)
            {
                enabled = false;
            }
        }


        void Awake()
        {
            m_NetworkCharacter = GetComponent<NetworkCharacterState>();
        }

        void FixedUpdate()
        {
            // TODO [GOMPS-82] replace with new Unity Input System 

            // Is mouse button pressed (not just checking for down to allow continuous movement inputs by holding the mouse button down)
            if (Input.GetMouseButton(0))
            {
                RaycastHit hit;

                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                {
                    // The MLAPI_INTERNAL channel is a reliable sequenced channel. Inputs should always arrive and be in order that's why this channel is used.
                    m_NetworkCharacter.InvokeServerRpc(m_NetworkCharacter.SendCharacterInputServerRpc, hit.point,
                        "MLAPI_INTERNAL");
                }
            }
        }
    }
}