using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Client
{
    [RequireComponent(typeof(BossRoom.NetworkCharacterState))]
    public class ClientCharacter : MLAPI.NetworkedBehaviour
    {
        //!!STUB. Client Character gamelogic will go here. 

        public override void NetworkStart()
        {
            if( !IsClient ) { this.enabled = false; }
        }

    }

}
