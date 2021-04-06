using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BossRoom.Scripts.Editor
{
    /// <summary>
    /// Class that permits auto-loading a bootstrap scene when the editor switches play state. This class is
    /// initialized when Unity loads and when scripts are recompiled. This is to be able to subscribe to
    /// EditorApplication's playModeStateChanged event, which is when we wish to open a new scene.
    /// </summary>
    [InitializeOnLoad]
    public class SceneBootstrapper
    {
        const string k_BootstrapSceneKey = "BootstrapScene";
        const string k_PreviousSceneKey = "PreviousScene";
        const string k_LoadBootstrapSceneKey = "LoadBootstrapScene";

        const string k_LoadBootstrapSceneOnPlay = "Boss Room/Load Bootstrap Scene On Play";
        const string k_DoNotLoadBootstrapSceneOnPlay = "Boss Room/Don't Load Bootstrap Scene On Play";

        static string BootstrapScene
        {
            get
            {
                if (!EditorPrefs.HasKey(k_BootstrapSceneKey))
                {
                    EditorPrefs.SetString(k_BootstrapSceneKey, EditorBuildSettings.scenes[0].path);
                }
                return EditorPrefs.GetString(k_BootstrapSceneKey, EditorBuildSettings.scenes[0].path);
            }
            set => EditorPrefs.SetString(k_BootstrapSceneKey, value);
        }

        static string PreviousScene
        {
            get => EditorPrefs.GetString(k_PreviousSceneKey);
            set => EditorPrefs.SetString(k_PreviousSceneKey, value);
        }

        static bool LoadBootstrapScene
        {
            get
            {
                if (!EditorPrefs.HasKey(k_LoadBootstrapSceneKey))
                {
                    EditorPrefs.SetBool(k_LoadBootstrapSceneKey, true);
                }
                return EditorPrefs.GetBool(k_LoadBootstrapSceneKey, true);
            }
            set => EditorPrefs.SetBool(k_LoadBootstrapSceneKey, value);
        }

        static SceneBootstrapper()
        {
            EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
        }

        [MenuItem(k_LoadBootstrapSceneOnPlay, true)]
        static bool ShowLoadBootstrapSceneOnPlay()
        {
            return !LoadBootstrapScene;
        }

        [MenuItem(k_LoadBootstrapSceneOnPlay)]
        static void EnableLoadBootstrapSceneOnPlay()
        {
            LoadBootstrapScene = true;
        }

        [MenuItem(k_DoNotLoadBootstrapSceneOnPlay, true)]
        static bool ShowDoNotLoadBootstrapSceneOnPlay()
        {
            return LoadBootstrapScene;
        }

        [MenuItem(k_DoNotLoadBootstrapSceneOnPlay)]
        static void DisableDoNotLoadBootstrapSceneOnPlay()
        {
            LoadBootstrapScene = false;
        }

        static void EditorApplicationOnplayModeStateChanged(PlayModeStateChange obj)
        {
            if (!LoadBootstrapScene)
            {
                return;
            }

            if (obj == PlayModeStateChange.ExitingEditMode)
            {
                // cache previous scene so we return to this scene after play session, if possible
                PreviousScene = EditorSceneManager.GetActiveScene().path;

                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    // user either hit "Save" or "Don't Save"; open bootstrap scene

                    if (!string.IsNullOrEmpty(BootstrapScene) &&
                        System.Array.Exists(EditorBuildSettings.scenes, scene => scene.path == BootstrapScene))
                    {
                        // scene is included in build settings; open it
                        EditorSceneManager.OpenScene(BootstrapScene);
                    }
                }
                else
                {
                    // user either hit "Cancel" or exited window; don't open bootstrap scene & return to editor

                    EditorApplication.isPlaying = false;
                }
            }
            else if (obj == PlayModeStateChange.EnteredEditMode)
            {
                if (!string.IsNullOrEmpty(PreviousScene))
                {
                    EditorSceneManager.OpenScene(PreviousScene);
                }
            }
        }
    }
}
