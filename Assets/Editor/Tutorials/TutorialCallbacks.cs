using UnityEngine;
using UnityEditor;
using Unity.Tutorials.Core.Editor;

namespace Unity.Netcode.Samples.BossRoom
{
    /// <summary>
    /// Implement your Tutorial callbacks here.
    /// </summary>
    [CreateAssetMenu(fileName = k_DefaultFileName, menuName = "Tutorials/" + k_DefaultFileName + " Instance")]
    public class TutorialCallbacks : ScriptableObject
    {
        [SerializeField]
        SceneAsset m_StartupScene;

        /// <summary>
        /// The default file name used to create asset of this class type.
        /// </summary>
        const string k_DefaultFileName = "TutorialCallbacks";

        /// <summary>
        /// Creates a TutorialCallbacks asset and shows it in the Project window.
        /// </summary>
        /// <param name="assetPath">
        /// A relative path to the project's root. If not provided, the Project window's currently active folder path is used.
        /// </param>
        /// <returns>The created asset</returns>
        public static ScriptableObject CreateAndShowAsset(string assetPath = null)
        {
            assetPath = assetPath ?? $"{TutorialEditorUtils.GetActiveFolderPath()}/{k_DefaultFileName}.asset";
            var asset = CreateInstance<TutorialCallbacks>();
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(assetPath));
            EditorUtility.FocusProjectWindow(); // needed in order to make the selection of newly created asset to really work
            Selection.activeObject = asset;
            return asset;
        }

        public void StartTutorial(Tutorial tutorial)
        {
            TutorialWindow.StartTutorial(tutorial);
        }

        public bool IsConnectedToUgs()
        {
            return CloudProjectSettings.projectBound;
        }

        public void ShowServicesSettings()
        {
            SettingsService.OpenProjectSettings("Project/Services");
        }

        public void OpenURL(string url)
        {
            TutorialEditorUtils.OpenUrl(url);
        }

        public void LoadStartupScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(m_StartupScene));
        }
    }
}
