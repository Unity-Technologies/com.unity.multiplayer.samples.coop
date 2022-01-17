using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class BakingMenu : MonoBehaviour
{

    [MenuItem("Boss Room/Lighting Setup/Environment Lights/Baked")]
    static void EnvironmentLightsBaked()
    {
        GameObject[] bakedLights = GameObject.FindGameObjectsWithTag("LightingBaked");
        GameObject[] realtimelights = GameObject.FindGameObjectsWithTag("LightingRealtime");
        
        foreach (var index in bakedLights)
        {
            index.GetComponent<Light>().enabled = true;
        }

        foreach (var index in realtimelights)
        {
            index.GetComponent<Light>().enabled = false;
        }
        
        Debug.Log(realtimelights.Length + " Environment lights set to Baked");
    }
    
    [MenuItem("Boss Room/Lighting Setup/Environment Lights/Realtime")]
    static void EnvironmentLightsRealtime()
    {
        GameObject[] bakedLights = GameObject.FindGameObjectsWithTag("LightingBaked");
        GameObject[] realtimelights = GameObject.FindGameObjectsWithTag("LightingRealtime");
        
        foreach (var index in bakedLights)
        {
            index.GetComponent<Light>().enabled = false;
        }

        foreach (var index in realtimelights)
        {
            index.GetComponent<Light>().enabled = true;
        }
        
        Debug.Log(bakedLights.Length + " Environment lights set to Realtime");
    }

    [MenuItem("Boss Room/Lighting Setup/Light Probes/Enabled")]
    static void LightProbesEnabled()
    {
        GameObject[] lightProbes = GameObject.FindGameObjectsWithTag("LightingProbes");

        if (lightProbes == null || lightProbes.Length == 0)
        {
            Debug.Log("No light probes found in scene");
        }

        else
        {
            foreach (var index in lightProbes)
            {
                index.GetComponent<LightProbeGroup>().enabled = true;
                Debug.Log("Light probes enabled");
            }
        }
    }
    
    [MenuItem("Boss Room/Lighting Setup/Light Probes/Disabled")]
    static void LightProbesDisabled()
    {
        GameObject[] lightProbes = GameObject.FindGameObjectsWithTag("LightingProbes");

        if (lightProbes == null || lightProbes.Length == 0)
        {
            Debug.Log("No light probes found in scene");
        }

        else
        {
            foreach (var index in lightProbes)
            {
                index.GetComponent<LightProbeGroup>().enabled = false;
                Debug.Log("Light probes disabled");
            }
        }
    }
    
    [MenuItem("Boss Room/Lighting Setup/Reflection Probes/Baked")]
    static void ReflectionProbesBaked()
    {
        GameObject[] reflectionProbes = GameObject.FindGameObjectsWithTag("LightingReflectionProbe");

        if (reflectionProbes == null || reflectionProbes.Length == 0)
        {
            Debug.Log("No reflection probes found in scene");
        }

        else
        {
            foreach (var index in reflectionProbes)
            {
                index.GetComponent<ReflectionProbe>().mode = ReflectionProbeMode.Baked;
                Debug.Log("Reflection probes set to Baked");
            }  
        }
    }
    
    [MenuItem("Boss Room/Lighting Setup/Reflection Probes/Realtime")]
    static void ReflectionProbesRealtime()
    {
        GameObject[] reflectionProbes = GameObject.FindGameObjectsWithTag("LightingReflectionProbe");
        
        if (reflectionProbes == null || reflectionProbes.Length == 0)
        {
            Debug.Log("No reflection probes found in scene");
        }

        else
        {
            foreach (var index in reflectionProbes)
            {
                index.GetComponent<ReflectionProbe>().mode = ReflectionProbeMode.Realtime;
                Debug.Log("Reflection probes set to Realtime");
            }  
        }
    }
    
    [MenuItem("Boss Room/Lighting Setup/All Baked")]
    static void AllBaked()
    {
        EnvironmentLightsBaked();
        ReflectionProbesBaked();
        Debug.Log("All lights and reflection probes in scene set to Baked");
    }
    
    [MenuItem("Boss Room/Lighting Setup/All Realtime (except area lights)")]
    static void SetAllRealtime()
    {
        EnvironmentLightsRealtime();
        ReflectionProbesRealtime();
        Debug.Log("All lights and reflection probes in scene set to Realtime");
    }
    
    [MenuItem("Boss Room/Lighting Setup/Clear ALL Baked Data")]
    static void ClearBakedData()
    {
        Lightmapping.Clear();
        Debug.Log("All baked data cleared");
    }
}
