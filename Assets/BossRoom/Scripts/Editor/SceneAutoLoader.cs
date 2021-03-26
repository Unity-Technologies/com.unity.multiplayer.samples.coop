using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BossRoom.Scripts.Editor
{
    /// <summary>
    /// Scene auto-loading editor script. By default, it stores the scene at build index 0 as the default master
    /// scene as a player pref. The master scene can be overriden through the editor under
    /// Boss Room/Scene Autoload/Select Master Scene...
    /// Scene auto-loading is enabled by default and can be toggled off & back on through
    /// Boss Room/Scene Autoload/Don't Load Master On Play & similarly Boss Room/Scene Autoload/Load Master On Play.
    /// </summary>
    /// <remarks> This is based off the implementation here: http://wiki.unity3d.com/index.php/SceneAutoLoader
    /// License can also be found here: https://creativecommons.org/licenses/by-sa/3.0/ </remarks>
    [InitializeOnLoad]
    static class SceneAutoLoader
    {
        // Properties are stored as editor preferences.
        const string k_EditorPrefLoadMasterOnPlay = "AutoLoadMasterScene";
        const string k_EditorPrefMasterScene = "MasterScene";
        const string k_EditorPrefPreviousScene = "PreviousScene";

        const string k_SelectMasterScene = "Boss Room/Scene Autoload/Select Master Scene...";
        const string k_LoadMasterOnPlay = "Boss Room/Scene Autoload/Load Master On Play";
        const string k_DontLoadMasterOnPlay = "Boss Room/Scene Autoload/Don't Load Master On Play";

        static bool LoadMasterOnPlay
        {
            get
            {
                // if key not registered under player prefs, make sure it is defaulted to true
                if (EditorPrefs.HasKey(k_EditorPrefLoadMasterOnPlay))
                {
                    return EditorPrefs.GetBool(k_EditorPrefLoadMasterOnPlay, true);
                }
                else
                {
                    EditorPrefs.SetBool(k_EditorPrefLoadMasterOnPlay, true);
                    return true;
                }
            }
            set => EditorPrefs.SetBool(k_EditorPrefLoadMasterOnPlay, value);
        }

        static string MasterScene
        {
            get => EditorPrefs.GetString(k_EditorPrefMasterScene, EditorBuildSettings.scenes[0].path);
            set => EditorPrefs.SetString(k_EditorPrefMasterScene, value);
        }

        static string PreviousScene
        {
            get => EditorPrefs.GetString(k_EditorPrefPreviousScene, EditorSceneManager.GetActiveScene().path);
            set => EditorPrefs.SetString(k_EditorPrefPreviousScene, value);
        }

        // Static constructor binds a playmode-changed callback.
        // [InitializeOnLoad] above makes sure this gets executed.
        static SceneAutoLoader()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        // Menu items to select the master scene and control whether or not to load it.
        [MenuItem(k_SelectMasterScene)]
        static void SelectMasterScene()
        {
            var masterScene = EditorUtility.OpenFilePanel("Select Master Scene", Application.dataPath, "unity");
            masterScene = masterScene.Replace(Application.dataPath, "Assets"); //project relative instead of absolute path
            if (!string.IsNullOrEmpty(masterScene))
            {
                MasterScene = masterScene;
                LoadMasterOnPlay = true;
            }
        }

        [MenuItem(k_LoadMasterOnPlay, true)]
        static bool ShowLoadMasterOnPlay()
        {
            return !LoadMasterOnPlay;
        }

        [MenuItem(k_LoadMasterOnPlay)]
        static void EnableLoadMasterOnPlay()
        {
            LoadMasterOnPlay = true;
        }

        [MenuItem(k_DontLoadMasterOnPlay, true)]
        static bool ShowDontLoadMasterOnPlay()
        {
            return LoadMasterOnPlay;
        }

        [MenuItem(k_DontLoadMasterOnPlay)]
        static void DisableLoadMasterOnPlay()
        {
            LoadMasterOnPlay = false;
        }

        // Play mode change callback handles the scene load/reload.
        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!LoadMasterOnPlay)
            {
                return;
            }

            if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // User pressed play -- autoload master scene.
                PreviousScene = EditorSceneManager.GetActiveScene().path;
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    // no need to open master when user is currently the active scene
                    if (PreviousScene == MasterScene)
                    {
                        return;
                    }

                    // If master scene exist, opens that scene
                    var masterScene = System.Array.Find(EditorBuildSettings.scenes,
                        scene => scene.path == MasterScene);

                    if (masterScene != null)
                    {
                        EditorSceneManager.OpenScene(MasterScene);
                        return;
                    }

                    // else cancel play and throw error
                    Debug.LogError($"Error: scene not found: {MasterScene}");
                    EditorApplication.isPlaying = false;
                }
                else
                {
                    // User cancelled the save operation -- cancel play as well.
                    EditorApplication.isPlaying = false;
                }
            }

            // isPlaying check required because cannot OpenScene while playing
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // no need to re-open master when user opened master initially
                if (PreviousScene == MasterScene)
                {
                    return;
                }

                // User pressed stop -- reload previous scene.
                EditorSceneManager.OpenScene(PreviousScene);
            }
        }
    }
}
