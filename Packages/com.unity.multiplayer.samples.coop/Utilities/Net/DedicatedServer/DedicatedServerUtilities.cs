using System;
using UnityEngine;

namespace Unity.Multiplayer.Samples
{
    public static class DedicatedServerUtilities
    {
        public static bool IsServerBuildTarget
        {
            get
            {
#if UNITY_SERVER
                return true;
#else
                return false;
#endif
            }
        }

        // todo improve perf on this
        // todo find a way to disable full stack trace if needed, this could take a lot of resources.
        // Logging format should change following which logging analytics service you use. Elasticsearch could
        // require a different format than splunk for example.
        public static void Log(string message)
        {
            Debug.Log($"===[{DateTime.UtcNow}] {Time.realtimeSinceStartup}|{Time.time} - {message}");
        }
    }
}