namespace Unity.Multiplayer.Samples.BossRoom
{
    /// <summary>
    /// Connection state corresponding to a client who received a message from the server with a disconnect reason.
    /// Since our disconnect process runs in multiple steps host side, this state is the first step client side. This
    /// state simply waits for the actual disconnect, then transitions to the offline state.
    /// </summary>
    class DisconnectingWithReasonState : ConnectionState
    {
        public override void Enter() { }

        public override void Exit() { }

        public override void OnClientDisconnect(ulong _)
        {
            ConnectionManager.ChangeState(ConnectionManager.m_Offline);
        }

        public override void OnUserRequestedShutdown()
        {
            ConnectionManager.ChangeState(ConnectionManager.m_Offline);
        }
    }
}
