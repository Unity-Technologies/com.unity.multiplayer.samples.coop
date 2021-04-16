namespace BossRoom.Client
{
    /// <summary>
    /// Client state-logic for post-game screen. (We don't actually need to do anything here
    /// right now, but we inherit our base-class's OnApplicationQuit() handler.)
    /// </summary>
    public class ClientPostGameState : GameStateBehaviour
    {
        public override GameState ActiveState { get { return GameState.PostGame; } }

        protected override void Start()
        {
            base.Start();

            //it is common for the user to get dumped back to main menu from here (i.e., if the host decides not to play again), and
            //it is a little funny to display a "Connection to Host Lost" message in that case. The best thing would probably be to
            //display a "Host Abandoned the Game" message, but this would require some more plumbing (an RPC from the host before it quit,
            //containing that information).
            //In the meantime, we just set "UserRequested" to suppress the Disconnected error popup.
            ClientMainMenuState.SetTransitionReason(ClientMainMenuState.TransitionReason.UserRequested);
        }

        public override void NetworkStart()
        {
            base.NetworkStart();
            if (!IsClient)
            {
                enabled = false;
            }
        }

    }
}
