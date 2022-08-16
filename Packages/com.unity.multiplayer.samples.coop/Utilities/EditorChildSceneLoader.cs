using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// Allows setting a scene as a root scene and setting its child scenes. To use this, drag this component on any object in a scene to make that scene a root scene. In the background, ChildSceneLoader will automatically manage this.
/// </summary>
public class EditorChildSceneLoader : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField]
    public List<SceneAsset> ChildScenesToLoadConfig;

    const string k_MenuBase = "Boss Room/Child Scene Loader";

    void Update()
    {
        // DO NOT DELETE keep this so we can enable/disable this script... (used in ChildSceneLoader)
    }

    void SaveSceneSetup()
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

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);

            // hacky way to "expand" a scene. If someone found a cleaner way to do this, let me know
            var selection = new List<Object>();
            selection.AddRange(Selection.objects);
            selection.Add(scene.GetRootGameObjects()[0]);
            Selection.objects = selection.ToArray();
        }
    }

    [MenuItem(k_MenuBase + "/Save Scene Setup To Config")]
    static void DoSaveSceneSetupMenu()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        var wasDirty = activeScene.isDirty;
        var foundLoaders = GameObject.FindObjectsOfType<EditorChildSceneLoader>();
        EditorChildSceneLoader loader;
        if (foundLoaders.Length == 0)
        {
            // create loader in scene
            var supportingGameObject = new GameObject("YOU SHOULD NOT SEE THIS");
            supportingGameObject.hideFlags = HideFlags.DontSaveInBuild | HideFlags.HideInHierarchy; // object only findable through scripts
            loader = supportingGameObject.AddComponent<EditorChildSceneLoader>();
            SceneManager.MoveGameObjectToScene(supportingGameObject, activeScene);
        }
        else
        {
            loader = foundLoaders[0];
        }

        loader.SaveSceneSetup();
        EditorSceneManager.MarkSceneDirty(loader.gameObject.scene);
        TrySaveScene(wasDirty, activeScene);
        PrintConfig();
    }

    // wasDirty: was the scene dirty before modifying it? if not, will try to save it directly without asking the user
    static void TrySaveScene(bool wasDirty, Scene activeScene)
    {
        if (!wasDirty)
        {
            EditorSceneManager.SaveScene(activeScene);
        }
        else
        {
            EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { activeScene });
        }
    }

    static EditorChildSceneLoader TryFindLoader()
    {
        var foundLoaders = GameObject.FindObjectsOfType<EditorChildSceneLoader>();
        if (foundLoaders.Length > 1)
        {
            throw new Exception("not normal, should only have one loaded child scene loader");
        }

        if (foundLoaders.Length == 0)
        {
            throw new Exception("couldn't find any child scene loader, please use Save Scene setup");
        }

        return foundLoaders[0];
    }

    [MenuItem(k_MenuBase + "/Remove Config")]
    static void RemoveConfig()
    {
        var foundObj = TryFindLoader().gameObject;
        var parentScene = foundObj.scene;
        var wasDirty = parentScene.isDirty;
        DestroyImmediate(foundObj);
        EditorSceneManager.MarkSceneDirty(parentScene);
        TrySaveScene(wasDirty, parentScene);
    }

    [MenuItem(k_MenuBase + "/Load Scene Setup from Config")]
    static void DoResetSceneToConfig()
    {
        TryFindLoader().ResetSceneSetupToConfig();
    }

    [MenuItem(k_MenuBase + "/Print Current Config")]
    static void PrintConfig()
    {
        var foundLoader = TryFindLoader();
        string toPrint = $"To Load ({foundLoader.ChildScenesToLoadConfig.Count}): ";
        foreach (var config in foundLoader.ChildScenesToLoadConfig)
        {
            toPrint += $"{config.name}, ";
        }

        Debug.Log(toPrint);
    }
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
