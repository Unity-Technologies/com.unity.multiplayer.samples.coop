using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

#if UNITY_EDITOR
using System.Security.Cryptography;
using System.Text;
#endif

using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Shared
{
    public class ProfileManager
    {
        public const string AuthProfileCommandLineArg = "-AuthProfile";

        static readonly string k_SavePath = Application.persistentDataPath + "/profiles.save";

        string m_Profile;

        public string Profile
        {
            get
            {
                return m_Profile ??= GetProfile();
            }
            set
            {
                if (!m_AvailableProfiles.Contains(value))
                {
                    m_AvailableProfiles.Add(value);
                    SaveProfiles();
                }

                m_Profile = value;
                onProfileChanged?.Invoke();
            }
        }

        public event Action onProfileChanged;

        List<String> m_AvailableProfiles;

        public List<String> AvailableProfiles
        {
            get
            {
                if (m_AvailableProfiles == null)
                {
                    LoadProfiles();
                }

                return m_AvailableProfiles;
            }
        }

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

            // When running in the Editor make a unique ID from the Application.dataPath.
            // This will work for cloning projects manually, or with Virtual Projects.
            // Since only a single instance of the Editor can be open for a specific
            // dataPath, uniqueness is ensured.
            var hashedBytes = new MD5CryptoServiceProvider()
                .ComputeHash(Encoding.UTF8.GetBytes(Application.dataPath));
            Array.Resize(ref hashedBytes, 16);
            return new Guid(hashedBytes).ToString("N");
#else
            return "";
#endif
        }

        void LoadProfiles()
        {
            if (File.Exists(k_SavePath))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(k_SavePath, FileMode.Open);
                m_AvailableProfiles = (List<string>)bf.Deserialize(file);
                file.Close();
            }
            else
            {
                m_AvailableProfiles = new List<string>();
            }
        }

        void SaveProfiles()
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(k_SavePath);
            bf.Serialize(file, m_AvailableProfiles);
            file.Close();
        }

    }
}
