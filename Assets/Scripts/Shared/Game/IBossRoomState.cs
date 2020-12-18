using System;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom
{
    /// <summary>
    /// Interface that models a single state of the BossRoom game. See BossRoomStateManager for more information. 
    /// </summary>
    interface IBossRoomState
    {
        /// <summary>
        /// Called when this BossRoomState is transitioned to.
        /// <param name="stateParams"/>optional dictionary of parameters to be used by the new gamestate. </param>
        /// </summary>
        void Initialize( BossRoomStateManager manager, Dictionary<string,System.Object> stateParams=null );
        
        /// <summary>
        /// Called once per Update by the BossRoomStateManager. 
        /// </summary>
        void Update();

        /// <summary>
        /// Called when this BossRoomState ends. 
        /// </summary>
        void Destroy();
        
        /// <summary>
        /// What BossRoomState this object represents. 
        /// </summary>
        BossRoomState State { get; }
    }
}
