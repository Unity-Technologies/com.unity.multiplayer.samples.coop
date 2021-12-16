using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.Utilities
{
    [CreateAssetMenu]
    public class SceneEntry : ScriptableObject
    {
#if UNITY_EDITOR
        public SceneAsset SceneAssetToLoad;

        private void OnValidate()
        {
            if (SceneAssetToLoad != null)
            {
                SceneName = SceneAssetToLoad.name;
            }
        }
#endif
        public bool EndsLoadingScreen;

        public bool TriggersLoadingScreen;

        [HideInInspector]
        public string SceneName;
    }
}
