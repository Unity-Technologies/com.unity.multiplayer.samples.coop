using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Should be placed inside root scene and setup with child scenes to load. ChildSceneLoader will take care of the rest
/// </summary>
public class EditorChildSceneLoader : MonoBehaviour
{
    [SerializeField]
    public List<SceneAsset> ChildScenesToLoad;
#if UNITY_EDITOR

#endif
}

#if UNITY_EDITOR
[InitializeOnLoad]
public class ChildSceneLoader
{
    static ChildSceneLoader()
    {
        EditorSceneManager.sceneOpened += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene currentSceneLoaded, OpenSceneMode mode)
    {
        if (mode != OpenSceneMode.Single) return; // only for root scenes loading asd

        var scenesToLoadObjects = GameObject.FindObjectsOfType<EditorChildSceneLoader>();
        if (scenesToLoadObjects.Length > 1)
        {
            throw new Exception("Should only have one root scene at once loaded");
        }

        if (scenesToLoadObjects.Length == 0) // only when we have a config
        {
            return;
        }

        var sceneAssetsToLoad = scenesToLoadObjects[0].ChildScenesToLoad;

        List<SceneSetup> sceneSetupToLoad = new();
        sceneSetupToLoad.Add(new SceneSetup() { path = currentSceneLoaded.path, isActive = true, isLoaded = true });
        foreach (var sceneAsset in sceneAssetsToLoad)
        {
            sceneSetupToLoad.Add(new SceneSetup() { path = AssetDatabase.GetAssetPath(sceneAsset), isActive = false, isLoaded = true });
        }

        EditorSceneManager.RestoreSceneManagerSetup(sceneSetupToLoad.ToArray());
        Debug.Log("Setup done for root scene and child scenes");
    }
}
#endif