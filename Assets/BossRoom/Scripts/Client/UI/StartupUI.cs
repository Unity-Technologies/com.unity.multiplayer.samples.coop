using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossRoom.Client
{
    /// <summary>
    /// Provides backing logic for any UI before MainMenu stage. Mostly we just load main menu
    /// </summary>
    public class StartupUI : MonoBehaviour
    {
        void Start()
        {
            AudioListener.volume = ClientPrefs.GetMasterVolume();
            SceneManager.LoadScene("MainMenu");
        }
    }
}

