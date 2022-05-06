namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Client specialization of core BossRoom game logic.
    /// Empty for now, holds DI scope for that state (from parent class)
    /// </summary>
    public class ClientBossRoomState : GameStateBehaviour
    {
        public override GameState ActiveState { get { return GameState.BossRoom; } }

        // TODO still needed?
        // public override void OnNetworkSpawn()
        // {
        //     if (!IsClient) { this.enabled = false; }
        // }
    }
}
