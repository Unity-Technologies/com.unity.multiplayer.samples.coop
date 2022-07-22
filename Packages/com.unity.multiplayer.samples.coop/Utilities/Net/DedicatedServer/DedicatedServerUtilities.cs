using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

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

        /// <summary>
        /// Some notes on logging and dedicated servers:
        ///
        /// With dedicated server, you don't have a view into your game like you'd have with other types of platforms. You'll rely a lot on logging to get
        /// insights into what's happening on your servers, even more with your live fleets of thousands of servers.
        /// Unity's default logging isn't usable for dedicated server use cases. The following requirements are missing:
        /// - Structured logging: with 1000+ server fleets, you don't want to look at log files individually. You'll need log ingestion/analysis tools to be able
        /// to parse all that data (think tools like elasticsearch for example). Making your logs structured so they are machine friendly (for example
        /// having each log entry be a json object) makes this easier to integrate with those tools and easier to analyze.
        /// - Log levels: most of the time, you won't want to receive info or debug logs, only warnings and errors (so you don't spam your analysis tools and so they don't
        /// cost your a fortune). However, you'll also want to enable info and debug logs for specific servers when debugging them. Having a logger that
        /// manages this for you is better than wrapping your logs yourself and managing this yourself.
        /// - Log file rotation: Dedicated servers can run for days and days, while games on user devices will run for a few play sessions before being closed. This means your
        /// log file will grow and grow. Rotation is an automated way to swap log files each x hours or days. This allows deleting and easier managing
        /// of older log files.
        /// - Performance: logging can be a performance costly operation, which contributes to your CPU perf costs, which in turn are translated to hosting monetary costs.
        /// Having a logging library that's optimized for these scenarios is essential (burstable, threaded, etc).
        /// This also includes not having to print full stack traces (not needed for most devops operations, but could be enabled for debugging)
        ///
        /// A few solutions exists for this. ZLogger https://github.com/Cysharp/ZLogger and serilog https://www.nuget.org/packages/serilog/ for example.
        /// Unity also has an experimental package com.unity.logging that answers the above needs as well. Once this is out of experimental, this will be
        /// integrated in boss room. TODO for this first pass at DGS, we're using the default logger. TODO replace me - MTT-4038
        /// </summary>
        /// <param name="message"></param>
        public static void Log(string message)
        {
            // IMPORTANT FOR LOGGING READ ABOVE. The following isn't recommended for production use with dedicated game servers.
            Debug.Log($"[{DateTime.UtcNow}] {Time.realtimeSinceStartup} {Time.time} pid[{Process.GetCurrentProcess().Id}] - {message}");
        }

        /// <summary>
        /// Quick tool to get insights in your current scene, to be able to debug client and server hierarchies.
        /// </summary>
        public static void PrintSceneHierarchy()
        {
            List<GameObject> rootObjects = new List<GameObject>();
            Scene scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(rootObjects);
            string toPrint = "\n";
            foreach (var rootObject in rootObjects)
            {
                toPrint += $"{GetInfoForObject(rootObject)}\n";
                PrintChildObjectsRecursive(rootObject, depth: 0, ref toPrint);
            }

            Log(toPrint);
        }

        private static void PrintChildObjectsRecursive(GameObject parentObject, int depth, ref string toPrint)
        {
            if (parentObject.transform.childCount == 0)
            {
                return;
            }

            string tabs = new string(' ', ++depth * 4);
            foreach (Transform child in parentObject.transform)
            {
                toPrint += $"{tabs}{GetInfoForObject(child.gameObject)}\n";
                PrintChildObjectsRecursive(child.gameObject, depth, ref toPrint); // asdf
            }
        }

        private static string GetInfoForObject(GameObject obj)
        {
            List<Component> allComponents = new();
            obj.GetComponents(allComponents);
            var nullCount = allComponents.FindAll(component => component == null).Count;
            return $"{obj.name}\tnb null {nullCount}/{allComponents.Count}";
        }
    }
}
