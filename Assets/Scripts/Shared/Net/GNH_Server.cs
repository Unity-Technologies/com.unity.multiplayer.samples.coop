using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom
{
    public class GNH_Server
    {
        private GameNetHub m_hub;

        public GNH_Server(GameNetHub hub)
        {
            m_hub = hub;
        }
        public void RecvRequestConnect(string guid, ulong clientId)
        {
            //TODO: maintain a mapping of clientID to GUID, and handle the reconnect case. 

            //for the moment, just accept the change and point the client to "SampleScene". We will replace this
            //with logic that selects between the Load 
            m_hub.S2C_ConnectFinished(clientId, "SampleScene", ConnectStatus.CONNECT);
        }

    }
}

