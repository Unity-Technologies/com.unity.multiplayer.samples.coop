using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

namespace BossRoom
{
    /// <summary>
    /// Common data and RPCs for the CharSelect stage. 
    /// </summary>
    public class CharSelectData : NetworkedBehaviour
    {
        //TODO: GOMPS-83. implement the true synced array for CharacterSlots. There should be 8, and some can be null. 
        //They can probably just be strings for now. 
        private MLAPI.NetworkedVar.Collections.NetworkedList<string> CharSlots;

        public override void NetworkStart()
        {
            base.NetworkStart();
            CharSlots = new MLAPI.NetworkedVar.Collections.NetworkedList<string>();

        }

    }

}

