using System;
using System.Collections;
using NUnit.Framework;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Multiplayer.Samples.BossRoom.Tests.Runtime
{
    [ConditionalIgnore(DedicatedServerTestUtilities.ServerOnly, "Ignored on Client.")]
    public class DedicatedServerFullFlowTest
    {
        [UnityTest]
        public IEnumerator TestStartToLobbyFlow()
        {
            SceneManager.LoadSceneAsync(SceneNames.Startup);

            // validate the loading of project's Bootstrap scene
            yield return new TestUtilities.WaitForSceneLoad(SceneNames.Startup);

            // MainMenu is loaded as soon as Startup scene is launched, validate it is loaded
            yield return new TestUtilities.WaitForSceneLoad(SceneNames.DedicatedServerLobbyManagement);

            // MainMenu is loaded as soon as Startup scene is launched, validate it is loaded
            yield return new TestUtilities.WaitForSceneLoad(SceneNames.CharSelect);

            // Trying to simulate a client connecting would involve stubbing pretty much all of boss room... not gonna do that, this test setup stops here
            // We can still do basic tests, seeing if dedicated server can at least be started automatically with no errors

            Assert.That(NetworkManager.Singleton.IsListening);
            Assert.That(NetworkManager.Singleton.IsServer);
            Assert.False(NetworkManager.Singleton.IsClient);
        }

        [UnityTearDown]
        public IEnumerator DestroySceneGameObjects()
        {
            SceneManager.LoadScene("EmptyScene", LoadSceneMode.Single);
            yield return new TestUtilities.WaitForSceneLoad("EmptyScene");
        }
    }
}
