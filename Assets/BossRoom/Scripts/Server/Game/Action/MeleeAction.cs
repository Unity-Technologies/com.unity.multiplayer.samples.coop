using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Server
{
    public class MeleeAction : Action
    {
        private bool m_ExecFired;

        public MeleeAction(ServerCharacter parent, ref ActionRequestData data, int level) : base(parent, ref data, level)
        {
        }

        public override bool Start()
        {
            m_Parent.NetState.ServerBroadcastAction(ref Data);
            return true;
        }

        public override bool Update()
        {
            if( !m_ExecFired && (Time.time-TimeStarted) >= Description.ExecTime_s )
            {
                m_ExecFired = true;
                var foe = DetectFoe();
                if(foe != null )
                {
                    foe.RecieveHP(this.m_Parent, -Description.Amount);
                }
            }


            return true; 
        }



        /// <summary>
        /// Returns the ServerCharacter of the foe we hit, or null if none found. 
        /// </summary>
        /// <returns></returns>
        private ServerCharacter DetectFoe()
        {
            //this simple detect just does a boxcast out from our position in the direction we're facing, out to the range of the attack. 

            var my_bounds = this.m_Parent.GetComponent<Collider>().bounds;

            RaycastHit hit;

            //NPCs (monsters) can hit PCs, and vice versa. No friendly fire allowed on either side. 
            int mask = LayerMask.GetMask(m_Parent.IsNPC ? "PCs" : "NPCs");

            if( Physics.BoxCast(m_Parent.transform.position, my_bounds.extents, m_Parent.transform.forward, out hit, Quaternion.identity, 
                Description.Range, mask ))
            {
                var foe_character = hit.collider.GetComponent<ServerCharacter>();
                return foe_character;
            }

            return null;
        }
    }
}
