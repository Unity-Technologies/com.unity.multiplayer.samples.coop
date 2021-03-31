using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossRoom.Client
{
    /// <summary>
    /// Provides backing logic for any UI before MainMenu stage. Mostly we just load main menu
    /// </summary>
    public class StartupUI : MonoBehaviour
    {
        [SerializeField]
        private ShaderVariantCollection m_PrewarmShaders;

        void Start()
        {
            AudioListener.volume = ClientPrefs.GetMasterVolume();

            m_PrewarmShaders.WarmUp();

            SceneManager.LoadScene("MainMenu");
        }
    }
}

