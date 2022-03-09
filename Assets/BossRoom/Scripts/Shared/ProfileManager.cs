using UnityEngine;

#if UNITY_EDITOR
using ParrelSync;
#endif

public static class ProfileManager
{
    public const string AuthProfileCommandLineArg = "-AuthProfile";

    static bool s_IsProfileSet = false;

    static string s_Profile = "";

    public static string Profile
    {
        get
        {
            if (!s_IsProfileSet)
            {
                s_Profile = GetProfile();
                s_IsProfileSet = true;
            }

            return s_Profile;
        }
        private set => s_Profile = value;
    }

    static string GetProfile()
    {
#if UNITY_EDITOR
            //The code below makes it possible for the clone instance to log in as a different user profile in Authentication service.
            //This allows us to test services integration locally by utilising Parrelsync.
            if (ClonesManager.IsClone())
            {
                Debug.Log("This is a clone project.");
                var customArguments = ClonesManager.GetArgument().Split(',');

                //second argument is our custom ID, but if it's not set we would just use some default.

                var hardcodedProfileID = customArguments.Length > 1 ? customArguments[1] : "defaultCloneID";

                return hardcodedProfileID;
            }
#else
        var arguments = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] == AuthProfileCommandLineArg)
            {
                var profileId = arguments[i + 1];
                return profileId;
            }
        }
#endif
        return "";
    }
}
