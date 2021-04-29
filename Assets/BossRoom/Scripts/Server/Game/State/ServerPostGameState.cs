using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BossRoom.Server
{

    /// <summary>
    /// The ServerPostGameState contains logic for 
    /// </summary>
    [RequireComponent(typeof(PostGameData))]
    public class ServerPostGameState : GameStateBehaviour
    {
        [SerializeField]
        private PostGameData m_PostGameData;

        public override GameState ActiveState { get { return GameState.PostGame; } }

        public override void NetworkStart()
        {
            base.NetworkStart();
            if (!IsServer)
            {
                enabled = false;
            }
            else
            {
                bool won = (bool)GameStateRelay.GetRelayObject();

                m_PostGameData.GameBannerState.Value =
                    (byte)(won ? PostGameData.BannerState.Won : PostGameData.BannerState.Lost);
            }
        }
    }
}
