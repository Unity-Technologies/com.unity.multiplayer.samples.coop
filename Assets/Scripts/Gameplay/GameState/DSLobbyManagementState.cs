using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    public class DSLobbyManagementState : GameStateBehaviour
    {
        [Inject]
        ConnectionManager m_ConnectionManager;
        public override GameState ActiveState => GameState.LobbyManagement;

        protected override void Start()
        {
            base.Start();

            // TODO create DGS lobby here, register to matchmaking, etc. This state bypasses the main menu setup users would normally do get in a game
            // and does its own game setup
            var address = "0.0.0.0";
            var port = 9998;
            Dictionary<string, string> args = new();
            foreach (var oneArg in Environment.GetCommandLineArgs())
            {
                var keyValue = oneArg.Split('=');
                args.Add(keyValue[0], keyValue.Length > 1 ? keyValue[1] : null);
            }

            var portArg = "-port";
            if (args.ContainsKey(portArg) && !int.TryParse(args[portArg], out port))
            {
                DedicatedServerUtilities.Log("failed to parse -port arg: " + args[portArg]);
            }

            DedicatedServerUtilities.Log($"Starting Headless Server, listening on address {address}:{port}");
            m_ConnectionManager.StartServerIP(address, port); // This will switch to the char select scene once the server started callback has been called
        }
    }}
