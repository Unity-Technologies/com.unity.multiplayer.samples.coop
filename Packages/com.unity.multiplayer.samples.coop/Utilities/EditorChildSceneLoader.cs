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

    public void ResetSceneSetupToConfig(bool askToSave)
    {
        var sceneAssetsToLoad = ChildScenesToLoadConfig;

        List<SceneSetup> sceneSetupToLoad = new List<SceneSetup>();
        foreach (var sceneAsset in sceneAssetsToLoad)
        {
            sceneSetupToLoad.Add(new SceneSetup() { path = AssetDatabase.GetAssetPath(sceneAsset), isActive = false, isLoaded = true });
        }

        sceneSetupToLoad[0].isActive = true;
        if (askToSave)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }
        EditorSceneManager.RestoreSceneManagerSetup(sceneSetupToLoad.ToArray());
    }

    public void ResetToRootSceneOnly()
    {
        var rootSceneSetup = new SceneSetup() { path = gameObject.scene.path, isActive = true, isLoaded = true};
        EditorSceneManager.RestoreSceneManagerSetup(new[] { rootSceneSetup });
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
            currentInspectorObject.ResetSceneSetupToConfig(askToSave: true);
        }
    }
}

[InitializeOnLoad]
public class ChildSceneLoader
{
    static ChildSceneLoader()
    {
        EditorSceneManager.sceneOpened += OnSceneLoaded;
        EditorApplication.playModeStateChanged += ExecuteUnloadChildOnPlay;
    }

    private static void ExecuteUnloadChildOnPlay(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            Debug.Log("Resetting to root scene. Runtime scripts should handle loading any child scene");
            TryGetRootSceneConfig()?.ResetToRootSceneOnly();
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            TryGetRootSceneConfig()?.ResetSceneSetupToConfig(askToSave: false);
        }
    }

    static void OnSceneLoaded(Scene _, OpenSceneMode mode)
    {
        if (mode != OpenSceneMode.Single || BuildPipeline.isBuildingPlayer) return; // try to load child scenes only for root scenes or if not building

        TryGetRootSceneConfig()?.ResetSceneSetupToConfig(askToSave: true);

        Debug.Log("Setup done for root scene and child scenes");
    }

    static EditorChildSceneLoader TryGetRootSceneConfig()
    {
        var scenesToLoadObjects = GameObject.FindObjectsOfType<EditorChildSceneLoader>();
        if (scenesToLoadObjects.Length > 1)
        {
            throw new Exception("Should only have one root scene at once loaded");
        }

        if (scenesToLoadObjects.Length == 0 || !scenesToLoadObjects[0].enabled) return null; // only when we have a config and when that config is enabled
        return scenesToLoadObjects[0];
    }
}
#endif
