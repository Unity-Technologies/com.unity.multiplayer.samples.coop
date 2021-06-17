using System;
using UnityEngine;

namespace BossRoom.Client
{
    [RequireComponent(typeof(NetworkCharacterState))]
    public class ClientCharacter : MLAPI.NetworkBehaviour
    {
        /// <summary>
        /// The Visualization GameObject isn't in the same transform hierarchy as the object, but it registers itself here
        /// so that the visual GameObject can be found from a NetworkObjectId.
        /// </summary>
        public Visual.ClientCharacterVisualization ChildVizObject { get; set; }

        public override void NetworkStart()
        {
            if (!IsClient)
            {
                enabled = false;
            }
        }
    }
}
