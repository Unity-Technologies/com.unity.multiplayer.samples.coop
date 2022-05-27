using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
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

        // todo improve perf on this
        // todo find a way to disable full stack trace if needed, this could take a lot of resources.
        // Logging format should change following which logging analytics service you use. Elasticsearch could
        // require a different format than splunk for example.
        public static void Log(string message)
        {
            Debug.LogFormat($"<b>===[{DateTime.UtcNow}]|{Time.realtimeSinceStartup}|{Time.time}|pid[{Process.GetCurrentProcess().Id}]</b> - {message}");
        }

        // [MenuItem("Debug/GetAll")]
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