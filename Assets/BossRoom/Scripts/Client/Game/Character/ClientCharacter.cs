using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Client
{
    [RequireComponent(typeof(BossRoom.NetworkCharacterState))]
    public class ClientCharacter : MLAPI.NetworkedBehaviour
    {
        public override void NetworkStart()
        {
            if( !IsClient ) { this.enabled = false; }
        }


        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
