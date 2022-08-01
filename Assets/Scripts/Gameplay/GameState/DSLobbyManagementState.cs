using System;
using System.Collections;
using System.Collections.Generic;
using BossRoom.Scripts.Shared.Utilities;
using Unity.Multiplayer.Samples.Utilities;
using UnityEngine;
using VContainer;

namespace Unity.Multiplayer.Samples.BossRoom.Server
{
    public class DSLobbyManagementState : GameStateBehaviour
    {
        [Inject]
        ConnectionManager m_ConnectionManager;
        public override GameState ActiveState => GameState.DedicatedServerLobbyManagement;

        protected override void Start()
        {
            base.Start();

            // TODO create DGS lobby here, register to matchmaking, etc. This state bypasses the main menu setup users would normally do get in a game
            // and does its own game setup MTT-4035
            var address = "0.0.0.0"; // Change this for fancier infrastructure hosting setup where you can listen on different IP addresses. Right now listening on all.
            var port = 9998;

            // Some quick command line processing.
            Dictionary<string, string> args = new();
            foreach (var oneArg in Environment.GetCommandLineArgs())
            {
                var keyValue = oneArg.Split('=');
                args.Add(keyValue[0], keyValue.Length > 1 ? keyValue[1] : null);
            }

            var portArg = "-port";
            if (args.ContainsKey(portArg) && !int.TryParse(args[portArg], out port))
            {
                DedicatedServerUtilities.LogCustom("failed to parse -port arg: " + args[portArg]);
            }

            IEnumerator StartServerCoroutine()
            {
                DedicatedServerUtilities.LogCustom($"Starting Headless Server, listening on address {address}:{port}");
                m_ConnectionManager.StartServerIP(address, port); // This will switch to the char select scene once the server started callback has been called

                yield return new WaitForServerStarted(); // Less performant than just the callback, but way more readable than a callback hell.

                // TODO change scene to char select here and do other init. why is it handled by connection manager right now?
                SceneLoaderWrapper.Instance.AddOnSceneEventCallback();
                SceneLoaderWrapper.Instance.LoadScene(SceneNames.CharSelect, useNetworkSceneManager: true);
            }

            StartCoroutine(StartServerCoroutine());
        }
    }
}
