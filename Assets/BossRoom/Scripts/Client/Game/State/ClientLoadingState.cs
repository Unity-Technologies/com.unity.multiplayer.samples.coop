using Unity.Multiplayer.Samples.BossRoom;
using UnityEngine;

public class ClientLoadingState : GameStateBehaviour
{
    public override GameState ActiveState { get { return GameState.Loading; } }
}
