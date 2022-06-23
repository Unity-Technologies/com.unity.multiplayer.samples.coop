using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Allows setting a scene as a root scene and setting its child scenes. To use this, drag this component on any object in a scene to make that scene a root scene. In the background, ChildSceneLoader will automatically manage this.
/// </summary>
public class EditorChildSceneLoader : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField]
    public List<SceneAsset> ChildScenesToLoadConfig;

    void Update()
    {
        // DO NOT DELETE keep this so we can enable/disable this script... (used in ChildSceneLoader)
    }

    public void SaveSceneSetup()
    {
        ChildScenesToLoadConfig ??= new List<SceneAsset>();
        ChildScenesToLoadConfig.Clear();
        foreach (var sceneSetup in EditorSceneManager.GetSceneManagerSetup())
        {
            ChildScenesToLoadConfig.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneSetup.path));
        }
    }

    public void ResetSceneSetupToConfig()
    {
        var sceneAssetsToLoad = ChildScenesToLoadConfig;

        List<SceneSetup> sceneSetupToLoad = new List<SceneSetup>();
        foreach (var sceneAsset in sceneAssetsToLoad)
        {
            sceneSetupToLoad.Add(new SceneSetup() { path = AssetDatabase.GetAssetPath(sceneAsset), isActive = false, isLoaded = true });
        }

        sceneSetupToLoad[0].isActive = true;
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.RestoreSceneManagerSetup(sceneSetupToLoad.ToArray());
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(EditorChildSceneLoader))]
public class ChildSceneLoaderInspectorGUI : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var currentInspectorObject = (EditorChildSceneLoader)target;

        if (GUILayout.Button("Save scene setup to config"))
        {
            currentInspectorObject.SaveSceneSetup();
        }

        if (GUILayout.Button("Reset scene setup from config..."))
        {
            currentInspectorObject.ResetSceneSetupToConfig();
        }
    }
}

[InitializeOnLoad]
public class ChildSceneLoader
{
    static ChildSceneLoader()
    {
        EditorSceneManager.sceneOpened += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene _, OpenSceneMode mode)
    {
        if (mode != OpenSceneMode.Single || BuildPipeline.isBuildingPlayer) return; // try to load child scenes only for root scenes or if not building

        var scenesToLoadObjects = GameObject.FindObjectsOfType<EditorChildSceneLoader>();
        if (scenesToLoadObjects.Length > 1)
        {
            throw new Exception("Should only have one root scene at once loaded");
        }

        if (scenesToLoadObjects.Length == 0 || !scenesToLoadObjects[0].enabled) // only when we have a config and when that config is enabled
        {
            return;
        }

        scenesToLoadObjects[0].ResetSceneSetupToConfig();

        Debug.Log("Setup done for root scene and child scenes");
    }
}
#endif
