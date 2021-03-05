using UnityEngine;


namespace BossRoom.Client
{
    /// <summary>
    /// Simple class to restart game theme on main menu load
    /// </summary>
    public class MainMenuMusicStarter : MonoBehaviour
    {
        void Start()
        {
            GameObject musicPlayerObj = GameObject.FindGameObjectWithTag("MusicPlayer");
            musicPlayerObj.GetComponent<ClientMusicPlayer>().RestartTheme();
        }
    }
}
