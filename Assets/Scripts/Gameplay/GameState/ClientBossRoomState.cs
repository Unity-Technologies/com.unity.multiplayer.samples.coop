namespace Unity.Multiplayer.Samples.BossRoom.Client
{

    /// <summary>
    /// Client specialization of core BossRoom game logic.
    /// </summary>
    public class ClientBossRoomState : GameStateBehaviour
    {
        public override GameState ActiveState { get { return GameState.BossRoom; } }
    }

}
