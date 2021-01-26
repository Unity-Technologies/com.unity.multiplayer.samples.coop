using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BossRoom;

namespace BossRoom.Server
{
    /// <summary>
    /// Server specialization of Character Select game state. 
    /// </summary>
    [RequireComponent(typeof(CharSelectData))]
    public class ServerCharSelectState : GameStateBehaviour
    {
        public override GameState ActiveState { get { return GameState.CharSelect; } }

        //TODO: GOMPS-83. Remove this temp variable and replace with core CharSelect logic. 
        private float m_start_s; //TEMP. manages transition. 

        public override void NetworkStart()
        {
            base.NetworkStart();
            if (!IsServer) { this.enabled = false; }

            m_start_s = Time.time;

        }

        // Update is called once per frame
        void Update()
        {
            if( (Time.time - m_start_s) > 3 )
            {
                //temp: we don't have any logic or anything in CharSelect, so for now we just skip on to the next scene.
                MLAPI.SceneManagement.NetworkSceneManager.SwitchScene("DungeonTest");
            }


        }
    }

}

