using UnityEngine;


namespace Unity.Multiplayer.Samples.BossRoom.Client
{
    /// <summary>
    /// Simple class to play game theme on scene load
    /// </summary>
    public class MainMenuMusicStarter : MonoBehaviour
    {
        // set whether theme should restart if already playing
        [SerializeField]
        bool m_Restart;

        void Start()
        {
            ClientMusicPlayer.Instance.PlayThemeMusic(m_Restart);
        }
    }
}
