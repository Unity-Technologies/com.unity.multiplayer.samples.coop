using System;
using System.Collections.Generic;
using System.Linq;
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
    public List<SceneAsset> ChildScenesToLoadConfig;

#if UNITY_EDITOR
    void Update()
    {
        // keep this so we can enable/disable this script... (used in ChildSceneLoader)
    }

    public void SaveSceneSetup()
    {
        ChildScenesToLoadConfig.Clear();
        foreach (var sceneSetup in EditorSceneManager.GetSceneManagerSetup())
        {
            ChildScenesToLoadConfig.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneSetup.path));
        }
    }

    public void ResetSceneSetupToConfig()
    {
        var sceneAssetsToLoad = ChildScenesToLoadConfig;

        List<SceneSetup> sceneSetupToLoad = new();
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
public class GameEventEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var currentInspectorObject = (EditorChildSceneLoader)target;

        if (GUILayout.Button("Save scene setup"))
        {
            currentInspectorObject.SaveSceneSetup();
        }

        if (GUILayout.Button("Reset from config..."))
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
        if (mode != OpenSceneMode.Single) return; // only for root scenes loading asd

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