using System;
using BossRoom.Scripts.Shared.Net.UnityServices.Lobbies;
using BossRoom.Scripts.Shared.Net.UnityServices.Relays;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure
{
    /// <summary>
    /// Ensure that message contents are obvious but not dependent on spelling strings correctly.
    /// </summary>
    public enum MessageType
    {
        // These are assigned arbitrary explicit values so that if a MessageType is serialized and more enum values are later inserted/removed, the serialized values need not be reassigned.
        // (If you want to remove a message, make sure it isn't serialized somewhere first.)
        None = 0,
        RenameRequest = 1,
        JoinLobbyRequest = 2,
        CreateLobbyRequest = 3,
        QueryLobbies = 4,
        QuickJoin = 5,

        //ChangeGameState = 100,
        ConfirmInGameState = 101,
        LobbyUserStatus = 102,
        //UserSetEmote = 103,
        ClientUserApproved = 104,
        ClientUserSeekingDisapproval = 105,
        EndGame = 106,

        StartCountdown = 200,
        CancelCountdown = 201,
        CompleteCountdown = 202,

        MinigameBeginning = 203,
        InstructionsShown = 204,
        MinigameEnding = 205,

        //DisplayErrorPopup = 300,
    }

    public struct EndGame
    {

    }

    public struct ChangeGameState
    {
        public GameState GameState;
        public ChangeGameState(GameState gameGameState)
        {
            GameState = gameGameState;
        }
    }

    public struct ConfirmInGameState
    {

    }

    public struct ClientUserSeekingDisapproval
    {
        public Action<Approval> DisapprovalAction;

        public ClientUserSeekingDisapproval(Action<Approval> disapprovalAction)
        {
            DisapprovalAction = disapprovalAction;
        }
    }

    public struct ClientUserApproved
    {

    }

    public struct DisplayErrorPopup
    {
        public string Message;
        public DisplayErrorPopup(string message)
        {
            Message = message;
        }
    }

    public struct StartCountdown
    {

    }

    public struct CancelCountdown
    {
    }

    public struct CompleteCountdown
    {
    }
}
