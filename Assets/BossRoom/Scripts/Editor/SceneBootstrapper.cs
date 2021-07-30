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
        const string k_LoadBootstrapSceneKey = "LoadBootstrapScene";

        const string k_LoadBootstrapSceneOnPlay = "Boss Room/Load Bootstrap Scene On Play";
        const string k_DoNotLoadBootstrapSceneOnPlay = "Boss Room/Don't Load Bootstrap Scene On Play";

        const string k_StartupScenePath = "Assets/BossRoom/Scenes/Startup.unity";

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
            set
            {
                // if value has changed, update playModeStartScene accordingly
                if (value != LoadBootstrapScene)
                {
                    SetPlayModeStartScene(value ? k_StartupScenePath : null);
                }

                EditorPrefs.SetBool(k_LoadBootstrapSceneKey, value);
            }
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

        static SceneBootstrapper()
        {
            SetPlayModeStartScene(LoadBootstrapScene ? k_StartupScenePath : null);
        }

        static void SetPlayModeStartScene(string scenePath)
        {
            SceneAsset sceneAsset = null;
            if (!string.IsNullOrEmpty(scenePath))
            {
                sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

                if (sceneAsset == null)
                {
                    Debug.LogError("Could not find scene asset at: " + scenePath);
                }
            }

            EditorSceneManager.playModeStartScene = sceneAsset;
        }
    }
}
