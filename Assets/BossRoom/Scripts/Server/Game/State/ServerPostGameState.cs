using System.Collections;
using UnityEngine;

namespace BossRoom.Server
{

    /// <summary>
    /// The ServerPostGameState contains logic for 
    /// </summary>
    [RequireComponent(typeof(PostGameData))]
    public class ServerPostGameState : GameStateBehaviour
    {
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
                StartCoroutine(CoroSetWinLoss(won));
            }
        }

        IEnumerator CoroSetWinLoss(bool won)
        {
            yield return new WaitForSeconds(1);
            GetComponent<PostGameData>().GameBannerState.Value =
                (byte)(won ? PostGameData.BannerState.Won : PostGameData.BannerState.Lost);
        }
    }
}
