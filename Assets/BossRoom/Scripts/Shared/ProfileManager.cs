using UnityEngine;

#if UNITY_EDITOR
using System;
using System.Security.Cryptography;
using System.Text;
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
            var arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (arguments[i] == AuthProfileCommandLineArg)
                {
                    var profileId = arguments[i + 1];
                    return profileId;
                }
            }

#if UNITY_EDITOR

            // When running in the Editor and not a ParrelSync clone, make a unique ID
            // from the Application.dataPath. This will work for cloning projects
            // manually, or with Virtual Projects. Since only a single instance of
            // the Editor can be open for a specific dataPath, uniqueness is ensured.
            var hashedBytes = new MD5CryptoServiceProvider()
                .ComputeHash(Encoding.UTF8.GetBytes(Application.dataPath));
            Array.Resize(ref hashedBytes, 16);
            return new Guid(hashedBytes).ToString("N");

#endif

            return "";
        }
    }
}
