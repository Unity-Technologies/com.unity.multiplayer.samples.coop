using UnityEngine;


namespace BossRoom.Client
{
    /// <summary>
    /// Simple class to start game theme in char select if not playing
    /// </summary>
    public class CharSelectMusicStarter : MonoBehaviour
    {
        void Start()
        {
            GameObject musicPlayerObj = GameObject.FindGameObjectWithTag("MusicPlayer");
            musicPlayerObj.GetComponent<ClientMusicPlayer>().PlayThemeMusic();
        }
    }
}
