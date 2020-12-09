using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// Client logic for the GameNetHub. Contains implementations for all of GameNetHub's S2C RPCs. 
    /// </summary>
    public class GNH_Client
    {
        private GameNetHub m_hub;

        public GNH_Client(GameNetHub hub)
        {
            m_hub = hub;
        }

        public void RecvConnectFinished(string targetScene, ConnectStatus status )
        {
            //TBD: switch to target scene. 
        }

    }
}
