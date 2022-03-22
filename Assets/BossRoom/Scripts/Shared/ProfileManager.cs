using UnityEngine;

#if UNITY_EDITOR
using System;
using System.Security.Cryptography;
using System.Text;
using ParrelSync;
#endif

namespace Unity.Multiplayer.Samples.BossRoom.Shared
{
    public static class ProfileManager
    {
        public const string AuthProfileCommandLineArg = "-AuthProfile";

        static string s_Profile;

        public static string Profile => s_Profile ??= GetProfile();

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

            // When running in the Editor and not a ParrelSync clone, make a unique ID
            // from the Application.dataPath. This will work for cloning projects
            // manually, or with Virtual Projects. Since only a single instance of
            // the Editor can be open for a specific dataPath, uniqueness is ensured.
            var hashedBytes = new MD5CryptoServiceProvider()
                .ComputeHash(Encoding.UTF8.GetBytes(Application.dataPath));
            Array.Resize(ref hashedBytes, 16);
            return new Guid(hashedBytes).ToString("N");

#endif

            var arguments = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (arguments[i] == AuthProfileCommandLineArg)
                {
                    var profileId = arguments[i + 1];
                    return profileId;
                }
            }

            return "";
        }
    }
}
