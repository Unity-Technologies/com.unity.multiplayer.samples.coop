using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Client
{
    /// <summary>
    /// Client-side of character movement game logic. 
    /// </summary>
    [RequireComponent(typeof(NetworkCharacterState), typeof(Rigidbody))]
    public class ClientCharacterMovement : MLAPI.NetworkedBehaviour
    {
        private NetworkCharacterState m_NetState;
        private Rigidbody m_Rigidbody;


        // Start is called before the first frame update
        void Start()
        {
            m_NetState = GetComponent<NetworkCharacterState>();
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        public override void NetworkStart()
        {
            if (IsServer)
            {
                //this component is not needed on the host (or dedicated server), because ServerCharacterMovement will directly
                //update the character's position. 
                this.enabled = false;
            }
        }

        // Update is called once per frame
        void Update()
        {
            transform.position = m_NetState.NetworkPosition.Value;
            transform.rotation = Quaternion.Euler(0, m_NetState.NetworkRotationY.Value, 0);

            m_Rigidbody.position = transform.position;
            m_Rigidbody.rotation = transform.rotation;
        }
    }
}

