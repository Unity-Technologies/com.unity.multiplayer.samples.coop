using UnityEditor;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Unity.Multiplayer.Samples.BossRoom.Tests.Runtime
{
    [InitializeOnLoad]
    public class DedicatedServerTestUtilities
    {
        public const string ServerOnly = "ServerOnly";
        static DedicatedServerTestUtilities()
        {
            var isServer = false;
#if UNITY_SERVER
            isServer = true;
#endif

            ConditionalIgnoreAttribute.AddConditionalIgnoreMapping(ServerOnly, !isServer);
        }
    }

    public class MyTestClass
    {
        [Test, ConditionalIgnore(DedicatedServerTestUtilities.ServerOnly, "Ignored on Client.")]
        public void TestRunningOnlyInServer()
        {
            Assert.Pass();
        }
    }
}
