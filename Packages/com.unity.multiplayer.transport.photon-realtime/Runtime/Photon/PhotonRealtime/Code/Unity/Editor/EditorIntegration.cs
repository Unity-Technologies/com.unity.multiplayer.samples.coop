// ----------------------------------------------------------------------------
// <copyright file="PhotonEditor.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
//   MenuItems and in-Editor scripts for PhotonNetwork.
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------

#if UNITY_2017_4_OR_NEWER
#define SUPPORTED_UNITY
#endif


#if !PHOTON_UNITY_NETWORKING && UNITY_EDITOR

namespace Photon.Realtime
{
    using UnityEditor;
    using UnityEditor.Compilation;
    

    public class EditorIntegration
    {
        protected static string DocumentationLocation = "Assets/Photon/PhotonNetworking-Documentation.pdf";

        protected static string UrlFreeLicense = "https://dashboard.photonengine.com/en-US/SelfHosted";

        public const string UrlDevNet = "https://doc.photonengine.com/en-us/pun/v2";

        protected static string UrlForum = "https://forum.photonengine.com";

        protected static string UrlCompare = "https://doc.photonengine.com/en-us/realtime/current/getting-started/onpremise-or-saas";

        protected static string UrlHowToSetup = "https://doc.photonengine.com/en-us/onpremise/current/getting-started/photon-server-in-5min";

        protected static string UrlAppIDExplained = "https://doc.photonengine.com/en-us/realtime/current/getting-started/obtain-your-app-id";

        public const string UrlCloudDashboard = "https://id.photonengine.com/en-US/account/signin?email=";

        public const string UrlPunSettings = "https://doc.photonengine.com/en-us/pun/v2/getting-started/initial-setup"; // the SeverSettings class has this url directly in it's HelpURL attribute.
        
        public const string UrlDiscordGeneral = "https://discord.gg/qP6XVe3XWK";
        
        public const string UrlRealtimeDocsOnline = "https://doc.photonengine.com/en-us/realtime/";



        [MenuItem("Window/Photon Realtime/Highlight Server Settings %#&p", false, 1)]
        protected static void MenuItemHighlightSettings()
        {
            // Pings PhotonServerSettings and makes it selected (show in Inspector)
            EditorGUIUtility.PingObject(PhotonAppSettings.Instance);
        }


        [UnityEditor.InitializeOnLoadMethod]
        public static void InitializeOnLoadMethod()
        {
            //Debug.Log("InitializeOnLoadMethod()"); // DEBUG

            EditorApplication.delayCall += OnDelayCall;
        }

        // used to register for various events (post-load)
        private static void OnDelayCall()
        {
            //Debug.Log("OnDelayCall()");// DEBUG
   
            EditorApplication.playModeStateChanged += PlayModeStateChanged;

            CompilationPipeline.assemblyCompilationStarted -= OnCompileStarted;
            CompilationPipeline.assemblyCompilationStarted += OnCompileStarted;


            #if (UNITY_2018 || UNITY_2018_1_OR_NEWER)
            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.projectChanged += OnProjectChanged;
            #else
            EditorApplication.projectWindowChanged -= OnProjectChanged;
            EditorApplication.projectWindowChanged += OnProjectChanged;
            #endif

            OnProjectChanged(); // call this initially from here, as the project change events happened earlier (on start of the Editor)
        }


        // called in editor, opens wizard for initial setup, keeps scene PhotonViews up to date and closes connections when compiling (to avoid issues)
        private static void OnProjectChanged()
        {
            //Debug.Log("OnProjectChanged()"); // DEBUG

            // Prevent issues with Unity Cloud Builds where ServerSettings are not found.
            // Also, within the context of a Unity Cloud Build, ServerSettings is already present anyway.
            #if UNITY_CLOUD_BUILD
            return;
            #endif

            // serverSetting is null when the file gets deleted. otherwise, the wizard should only run once and only if hosting option is not (yet) set
            if (!PhotonAppSettings.Instance.DisableAutoOpenWizard)
            {
                PhotonAppSettings.Instance.DisableAutoOpenWizard = true;
                
                // Marks settings object as dirty, so it gets saved.
                // unity 5.3 changes the usecase for SetDirty(). but here we don't modify a scene object! so it's ok to use
                EditorUtility.SetDirty(PhotonAppSettings.Instance);

                Photon.Realtime.Editor.WizardWindow.Open();     // would be nice to jump directly to the registration
            }
        }

        private static void OnCompileStarted(string obj)
        {
            // Photon should disconnect on compile
        }


        private static void PlayModeStateChanged(PlayModeStateChange state)
        {
            if (EditorApplication.isPlaying || !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (string.IsNullOrEmpty(PhotonAppSettings.Instance.AppSettings.AppIdRealtime) && !PhotonAppSettings.Instance.AppSettings.IsMasterServerAddress)
            {
                // TODO: show a dialog or log a warning?!
                //EditorUtility.DisplayDialog(CurrentLang.SetupWizardWarningTitle, CurrentLang.SetupWizardWarningMessage, CurrentLang.OkButton);
            }
        }
    }
}
#endif