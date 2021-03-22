using UnityEngine;

/// <summary>
/// Singleton class which saves/loads local-client settings.
/// (This is just a wrapper around the PlayerPrefs system,
/// so that all the calls are in the same place.)
/// </summary>
public class ClientPrefs
{
    public static float GetMasterVolume()
    {
        return PlayerPrefs.GetFloat("MasterVolume", 1);
    }

    public static void SetMasterVolume(float volume)
    {
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }
}
