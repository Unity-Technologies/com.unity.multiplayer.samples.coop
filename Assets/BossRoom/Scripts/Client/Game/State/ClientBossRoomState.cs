namespace Unity.Multiplayer.Samples.BossRoom.Client
{

    /// <summary>
    /// Client specialization of core BossRoom game logic.
    /// </summary>
    public class ClientBossRoomState : GameStateBehaviour
    {
        public override GameState ActiveState {  get { return GameState.BossRoom; } }


        public override void OnNetworkSpawn()
        {
            if( !IsClient ) { this.enabled = false; }
        }

    }

}
