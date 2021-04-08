using UnityEngine;

namespace BossRoom.Client
{
    /// <summary>
    /// Singleton class which saves/loads local-client settings.
    /// (This is just a wrapper around the PlayerPrefs system,
    /// so that all the calls are in the same place.)
    /// </summary>
    public class ClientPrefs
    {
        private const float k_DefaultMasterVolume = 1;
        private const float k_DefaultMusicVolume = 0.8f;

        public static float GetMasterVolume()
        {
            return PlayerPrefs.GetFloat("MasterVolume", k_DefaultMasterVolume);
        }

        public static void SetMasterVolume(float volume)
        {
            PlayerPrefs.SetFloat("MasterVolume", volume);
        }

        public static float GetMusicVolume()
        {
            return PlayerPrefs.GetFloat("MusicVolume", k_DefaultMusicVolume);
        }

        public static void SetMusicVolume(float volume)
        {
            PlayerPrefs.SetFloat("MusicVolume", volume);
        }

        /// <summary>
        /// Either loads a Guid string from Unity preferences, or creates one and checkpoints it, then returns it.
        /// </summary>
        /// <returns>The Guid that uniquely identifies this client install, in string form. </returns>
        public static string GetGuid()
        {
            if (PlayerPrefs.HasKey("client_guid"))
            {
                return PlayerPrefs.GetString("client_guid");
            }

            var guid = System.Guid.NewGuid();
            var guidString = guid.ToString();

            PlayerPrefs.SetString("client_guid", guidString);
            return guidString;
        }

    }
}
