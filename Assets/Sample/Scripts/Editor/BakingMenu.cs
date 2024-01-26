using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace Unity.BossRoom.Editor
{
    /// <summary>
    /// This is a script that creates a menu for baking lights (and changing other lighting features) for Boss Room.
    /// </summary>
    public abstract class BakingMenu
    {
        static void HandleEnvLights(bool realtimeLightsEnabled, bool bakedLightsEnabled, string lightingStatus)
        {
            var bakedLights = GameObject.FindGameObjectsWithTag("LightingBaked");
            var realtimeLights = GameObject.FindGameObjectsWithTag("LightingRealtime");

            foreach (var index in bakedLights)
            {
                index.GetComponent<Light>().enabled = bakedLightsEnabled;
            }

            foreach (var index in realtimeLights)
            {
                index.GetComponent<Light>().enabled = realtimeLightsEnabled;
            }

            Debug.Log("Environment lights set to " + lightingStatus + ": " + realtimeLights.Length);
        }

        static void HandleLightProbes(bool lightProbesEnabled, string lightProbesStatus)
        {
            var lightProbes = GameObject.FindGameObjectsWithTag("LightingProbes");

            if (lightProbes == null || lightProbes.Length == 0)
            {
                Debug.Log("No light probes found in scene");
            }
            else
            {
                foreach (var index in lightProbes)
                {
                    index.GetComponent<LightProbeGroup>().enabled = lightProbesEnabled;
                    Debug.Log("Light probes " + lightProbesStatus);
                }
            }
        }

        static void HandleReflectionProbes(ReflectionProbeMode reflectionProbeMode, string refProbesStatus)
        {
            var reflectionProbes = GameObject.FindGameObjectsWithTag("LightingReflectionProbe");

            if (reflectionProbes == null || reflectionProbes.Length == 0)
            {
                Debug.Log("No reflection probes found in scene");
            }
            else
            {
                foreach (var index in reflectionProbes)
                {
                    index.GetComponent<ReflectionProbe>().mode = reflectionProbeMode;
                    Debug.Log("Reflection probes set to " + refProbesStatus);
                }
            }
        }

        [MenuItem("Boss Room/Lighting Setup/Environment Lights/Baked")]
        static void SetEnvLightsBaked()
        {
            HandleEnvLights(false, true, "Baked");
        }

        [MenuItem("Boss Room/Lighting Setup/Environment Lights/Realtime (except area lights)")]
        static void SetEnvLightsRealtime()
        {
            HandleEnvLights(true, false, "Realtime");
        }

        [MenuItem("Boss Room/Lighting Setup/Light Probes/Enabled")]
        static void SetLightProbesEnabled()
        {
            HandleLightProbes(true, "enabled");
        }

        [MenuItem("Boss Room/Lighting Setup/Light Probes/Disabled")]
        static void SetLightProbesDisabled()
        {
            HandleLightProbes(false, "disabled");
        }

        [MenuItem("Boss Room/Lighting Setup/Reflection Probes/Baked")]
        static void SetRefProbesBaked()
        {
            HandleReflectionProbes(ReflectionProbeMode.Baked, "baked");
        }

        [MenuItem("Boss Room/Lighting Setup/Reflection Probes/Realtime")]
        static void SetRefProbesRealtime()
        {
            HandleReflectionProbes(ReflectionProbeMode.Realtime, "realtime");
        }

        [MenuItem("Boss Room/Lighting Setup/All Baked")]
        static void AllBaked()
        {
            SetEnvLightsBaked();
            SetRefProbesBaked();
            Debug.Log("All lights and reflection probes in scene set to Baked");
        }

        [MenuItem("Boss Room/Lighting Setup/All Realtime (except area lights)")]
        static void AllRealtime()
        {
            SetEnvLightsRealtime();
            SetRefProbesRealtime();
            Debug.Log("All lights (except for area lights) and reflection probes in scene set to Realtime");
        }
    }
}
