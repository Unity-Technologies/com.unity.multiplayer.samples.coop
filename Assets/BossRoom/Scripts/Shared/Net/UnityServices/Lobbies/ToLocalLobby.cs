using System.Collections.Generic;
using BossRoom.Scripts.Shared.Net.UnityServices.Game;
using LobbyRelaySample;
using Unity.Services.Lobbies.Models;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Lobbies
{
    /// <summary>
    /// Convert the lobby resulting from a request into a LocalLobby for use in the game logic.
    /// </summary>
    public static class ToLocalLobby
    {
        /// <summary>
        /// Create a new LocalLobby from the content of a retrieved lobby. Its data can be copied into an existing LocalLobby for use.
        /// </summary>
        public static void Convert(Lobby lobby, LocalLobby outputToHere)
        {
            LocalLobby.LobbyData info = new LocalLobby.LobbyData // Technically, this is largely redundant after the first assignment, but it won't do any harm to assign it again.
            {   LobbyID             = lobby.Id,
                LobbyCode           = lobby.LobbyCode,
                Private             = lobby.IsPrivate,
                LobbyName           = lobby.Name,
                MaxPlayerCount      = lobby.MaxPlayers,
                RelayCode           = lobby.Data?.ContainsKey("RelayCode") == true ? lobby.Data["RelayCode"].Value : null, // By providing RelayCode through the lobby data with Member visibility, we ensure a client is connected to the lobby before they could attempt a relay connection, preventing timing issues between them.
                RelayNGOCode        = lobby.Data?.ContainsKey("RelayNGOCode") == true ? lobby.Data["RelayNGOCode"].Value : null,
                State               = lobby.Data?.ContainsKey("State") == true ? (LobbyState) int.Parse(lobby.Data["State"].Value) : LobbyState.Lobby,
                Color               = lobby.Data?.ContainsKey("Color") == true ? (LobbyColor) int.Parse(lobby.Data["Color"].Value) : LobbyColor.None,
                State_LastEdit        = lobby.Data?.ContainsKey("State_LastEdit") == true ? long.Parse(lobby.Data["State_LastEdit"].Value) : 0,
                Color_LastEdit        = lobby.Data?.ContainsKey("Color_LastEdit") == true ? long.Parse(lobby.Data["Color_LastEdit"].Value) : 0,
                RelayNGOCode_LastEdit = lobby.Data?.ContainsKey("RelayNGOCode_LastEdit") == true ? long.Parse(lobby.Data["RelayNGOCode_LastEdit"].Value) : 0,
            };

            Dictionary<string, LobbyUser> lobbyUsers = new Dictionary<string, LobbyUser>();
            foreach (var player in lobby.Players)
            {
                // If we already know about this player and this player is already connected to Relay, don't overwrite things that Relay might be changing.
                if (player.Data?.ContainsKey("UserStatus") == true && int.TryParse(player.Data["UserStatus"].Value, out int status))
                {
                    if (status > (int)UserStatus.Connecting && outputToHere.LobbyUsers.ContainsKey(player.Id))
                    {
                        lobbyUsers.Add(player.Id, outputToHere.LobbyUsers[player.Id]);
                        continue;
                    }
                }

                // If the player isn't connected to Relay, get the most recent data that the lobby knows.
                // (If we haven't seen this player yet, a new local representation of the player will have already been added by the LocalLobby.)
                LobbyUser incomingData = new LobbyUser
                {
                    IsHost = lobby.HostId.Equals(player.Id),
                    DisplayName = player.Data?.ContainsKey("DisplayName") == true ? player.Data["DisplayName"].Value : default,
                    UserStatus  = player.Data?.ContainsKey("UserStatus") == true ? (UserStatus)int.Parse(player.Data["UserStatus"].Value) : UserStatus.Connecting,
                    ID = player.Id
                };
                lobbyUsers.Add(incomingData.ID, incomingData);
            }
            outputToHere.CopyObserved(info, lobbyUsers);
        }

        /// <summary>
        /// Create a list of new LocalLobbies from the result of a lobby list query.
        /// </summary>
        public static List<LocalLobby> Convert(QueryResponse response)
        {
            List<LocalLobby> retLst = new List<LocalLobby>();
            foreach (var lobby in response.Results)
                retLst.Add(Convert(lobby));
            return retLst;
        }
        private static LocalLobby Convert(Lobby lobby)
        {
            LocalLobby data = new LocalLobby();
            Convert(lobby, data);
            return data;
        }
    }
}
