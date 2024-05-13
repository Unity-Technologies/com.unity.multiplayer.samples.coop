using System.Collections;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Rendering;
using WebSocketSharp;
//using PackageInfo = UnityEditor.PackageInfo;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

#if UNITY_STANDALONE_OSX
using UnityEditor.OSXStandalone;
#endif

public class BuilderScript : MonoBehaviour
{
    static string ngoVersion = System.Environment.GetEnvironmentVariable("NGOVersion");
    
    [MenuItem("Tools/Builder/Build Android Vulkan")]
    static void BuildAndroidVulkan()
    {
        //enforcing the il2cpp backend
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        //enforcing Vulkan
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new []{GraphicsDeviceType.Vulkan});
        PlayerSettings.SetArchitecture(BuildTargetGroup.Android,1);
        AssetDatabase.SaveAssets();
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity","Assets/Scenes/Main.unity" };
        buildPlayerOptions.target = BuildTarget.Android;
        // SubTarget expects an integer.
        buildPlayerOptions.options = BuildOptions.Development | BuildOptions.ShowBuiltPlayer;
        buildPlayerOptions.locationPathName = "build/Android_Vulkan/TestBuild.apk";
        buildPlayerOptions.extraScriptingDefines = new[] { "NETCODE_DEBUG", "UNITY_CLIENT" };
      
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
    [MenuItem("Tools/Builder/Build Release Android Vulkan")]
    static void BuildReleaseAndroidVulkan()
    {
        //enforcing the il2cpp backend
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        //enforcing Vulkan
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new []{GraphicsDeviceType.Vulkan});
        PlayerSettings.SetArchitecture(BuildTargetGroup.Android,1);
        AssetDatabase.SaveAssets();
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity","Assets/Scenes/Main.unity" };
        buildPlayerOptions.target = BuildTarget.Android;
        // SubTarget expects an integer.
        buildPlayerOptions.options =  BuildOptions.ShowBuiltPlayer;
        buildPlayerOptions.locationPathName = "build/Android_Vulkan/TestBuild.apk";
        buildPlayerOptions.extraScriptingDefines = new[] { "NETCODE_DEBUG", "UNITY_CLIENT" };
      
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
    
    [MenuItem("Tools/Builder/Build Android With Server Vulkan")]
    static void BuildAndroidVulkanWithServer()
    {
        //enforcing the il2cpp backend
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        //enforcing Vulkan
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new []{GraphicsDeviceType.Vulkan});
        PlayerSettings.SetArchitecture(BuildTargetGroup.Android,1);
        AssetDatabase.SaveAssets();
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity","Assets/Scenes/Main.unity" };
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.Development;
        // SubTarget expects an integer.
        buildPlayerOptions.locationPathName = "build/Android_Vulkan_WithServer/TestBuild.apk";
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
    [MenuItem("Tools/Builder/Build Android GLES3")]
    static void BuildAndroidGLES3()
    {
        //enforcing the il2cpp backend
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        //enforcing GLES3
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new []{GraphicsDeviceType.OpenGLES3});
        PlayerSettings.SetArchitecture(BuildTargetGroup.Android,1);
//        NetCodeClientSettings.instance.ClientTarget = NetCodeClientTarget.Client;
        AssetDatabase.SaveAssets();
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity","Assets/Scenes/Main.unity" };
        buildPlayerOptions.target = BuildTarget.Android;
        // SubTarget expects an integer.
        buildPlayerOptions.options = BuildOptions.Development | BuildOptions.ShowBuiltPlayer;
        buildPlayerOptions.locationPathName = "build/Android_GLES/TestBuild.apk";
        buildPlayerOptions.extraScriptingDefines = new[] { "NETCODE_DEBUG", "UNITY_CLIENT" };
        
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
    
    [MenuItem("Tools/Builder/Build Dedicated Server Windows")]
    static void BuildDedicatedServerWindows()
    {
        //enforcing the il2cpp backend
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        AssetDatabase.SaveAssets();

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity","Assets/Scenes/Main.unity" };
        buildPlayerOptions.target = BuildTarget.StandaloneWindows;
        // SubTarget expects an integer.
        buildPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Server;
        //this is needed for profiling 
        //buildPlayerOptions.options = BuildOptions.Development;
        buildPlayerOptions.locationPathName = "build/Server-Win/Server.exe";

        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
    
    [MenuItem("Tools/Builder/Build Dedicated Server Linux")]
    static void BuildDedicatedServerLinux()
    {
        //enforcing the il2cpp backend
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Server, ScriptingImplementation.IL2CPP);
        AssetDatabase.SaveAssets();

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity","Assets/Scenes/Main.unity" };
        buildPlayerOptions.target = BuildTarget.StandaloneLinux64;
        // SubTarget expects an integer.
        buildPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Server;
        //this is needed for profiling 
        //buildPlayerOptions.options = BuildOptions.Development;
        buildPlayerOptions.locationPathName = "build/Server/Server.x86_64";

        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
    
    [MenuItem("Tools/Builder/Build Standalone Windows Vulkan")]
    static void BuildStandAloneWindowsVulkan()
    {
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
//        NetCodeClientSettings.instance.ClientTarget = NetCodeClientTarget.Client;
        //enforcing Vulkan
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new []{GraphicsDeviceType.Vulkan});
        AssetDatabase.SaveAssets();
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity","Assets/Scenes/Main.unity" };
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        //this is needed for profiling 
        buildPlayerOptions.options = BuildOptions.Development;
        buildPlayerOptions.locationPathName = "build/TestBuildWin.exe";
        buildPlayerOptions.extraScriptingDefines = new[] { "NETCODE_DEBUG", "UNITY_CLIENT" };
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
    
    [MenuItem("Tools/Builder/Build Standalone Windows DX11")]
    static void BuildStandAloneWindowsDX11()
    {
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
//        NetCodeClientSettings.instance.ClientTarget = NetCodeClientTarget.Client;
        //enforcing Dx11
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new []{GraphicsDeviceType.Direct3D11});
        AssetDatabase.SaveAssets();
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity","Assets/Scenes/Main.unity" };
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        //this is needed for profiling 
        buildPlayerOptions.options = BuildOptions.Development | BuildOptions.ShowBuiltPlayer;
        buildPlayerOptions.locationPathName = "build/TestBuildWin.exe";
        buildPlayerOptions.extraScriptingDefines = new[] { "NETCODE_DEBUG", "UNITY_CLIENT" };
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
    [MenuItem("Tools/Builder/Build Release Standalone Windows DX11")]
    static void BuildReleaseStandAloneWindowsDX11()
    {
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
//        NetCodeClientSettings.instance.ClientTarget = NetCodeClientTarget.Client;
        //enforcing Dx11
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new []{GraphicsDeviceType.Direct3D11});
        AssetDatabase.SaveAssets();
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity","Assets/Scenes/Main.unity" };
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        //this is needed for profiling 
        buildPlayerOptions.options =  BuildOptions.ShowBuiltPlayer;
        buildPlayerOptions.locationPathName = "build/TestBuildWin.exe";
        buildPlayerOptions.extraScriptingDefines = new[] { "NETCODE_DEBUG", "UNITY_CLIENT" };
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
    
    [MenuItem("Tools/Builder/Build Standalone Windows DX12")]
    static void BuildStandAloneWindowsDX12()
    {
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
//        NetCodeClientSettings.instance.ClientTarget = NetCodeClientTarget.Client;
        //enforcing DX12
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new []{GraphicsDeviceType.Direct3D12});
        AssetDatabase.SaveAssets();
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity","Assets/Scenes/Main.unity" };
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        //this is needed for profiling 
        buildPlayerOptions.options = BuildOptions.Development;
        buildPlayerOptions.locationPathName = "build/TestBuildWin.exe";
        buildPlayerOptions.extraScriptingDefines = new[] { "NETCODE_DEBUG", "UNITY_CLIENT" };
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
    
    private static string GetManifestPath()
    {
        return (Directory.GetCurrentDirectory() + "\\Packages\\manifest.json").Replace('\\',
            Path.DirectorySeparatorChar);
    }

    private static AddRequest request;
    [MenuItem("Tools/Builder/Update Unity Manifest")]
    [InitializeOnLoadMethod]
    static void UpdateUnityManifest()
    {
        if (ngoVersion.IsNullOrEmpty())
        {
            Debug.Log("ngoVersion is empty");
            EditorApplication.Exit(0);
            return;
        }
        // Construct the package name with the version
        //string packageId = $"{packageName}@{packageVersion}";
        var packageId = "com.unity.netcode.gameobjects@" + ngoVersion;
        
        // Check if the package is already installed
        if (!PackageExists(packageId))
        {
            // Start the package installation
            if (request != null && request.Status == StatusCode.InProgress)
            {
                return;
            }
            
            request = Client.Add(packageId);
            EditorApplication.Exit(0);
            //Debug.Log("Before Entering Update loop");
            //EditorApplication.update += ManifestUpdateLoop;
            // Register callback for when the installation is complete
            /*EditorApplication.update += () =>
            {
                if (request.IsCompleted)
                {
                    if (request.Status == StatusCode.Success)
                    {
                        Debug.Log("Package installed successfully!");
                    }
                    else
                    {
                        Debug.LogError("Failed to install package: " + request.Error.message);
                    }

                    EditorApplication.update -= () => { };
                    QuitUnity();
                }
                QuitUnity();
            };
            */
        }
        else
        {
            Debug.Log("Package is already installed.");
            EditorApplication.Exit(0);
        }
    }

    private static void ManifestUpdateLoop()
    {
       // AddRequest request = Client.Add(packageId);
       Debug.Log("Frank testing request status:" + request.Status);

        if (request.Status is StatusCode.Success or StatusCode.Failure)
        {
            if (request.Status == StatusCode.Success)
            {
                Debug.Log("Package installed successfully!");
            }
            else
            {
                Debug.LogError("Failed to install package: " + request.Error.message);
            }

            //EditorApplication.update -= () => { };
            EditorApplication.update -= ManifestUpdateLoop;
            EditorApplication.Exit(0);
            Application.Quit();
        }

        //Thread.Sleep(3000);
        //QuitUnity();
    }
    // Check if a package with the given ID is installed
    private static bool PackageExists(string packageId)
    {
        foreach (var package in PackageInfo.GetAllRegisteredPackages())
        {
            if (package.name == packageId)
            {
                return true;
            }
        }
        return false;
    }

    static IEnumerator WaitAndQuit()
    {
        yield return new WaitForSeconds(60); // Adjust the duration as needed
        QuitUnity();
    }

    static void QuitUnity()
    {
        EditorApplication.Exit(0);
    }
    
#if UNITY_STANDALONE_OSX
    private static void BuildStandAloneOSX(OSArchitecture macOSArch, BuildOptions buildOption)
    {
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Standalone, Il2CppCompilerConfiguration.Release);
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Standalone, Il2CppCodeGeneration.OptimizeSpeed);
        
//        NetCodeClientSettings.instance.ClientTarget = NetCodeClientTarget.Client;
        //enforcing Metal Graphics API
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX, new []{GraphicsDeviceType.Metal});
        AssetDatabase.SaveAssets();
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.options = buildOption;
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/SampleScene.unity","Assets/Scenes/SampleScene.unity" };
        buildPlayerOptions.target = BuildTarget.StandaloneOSX;
        
        // Select MacOS build target
        UserBuildSettings.architecture = macOSArch;
        
        buildPlayerOptions.locationPathName = "build/TestBuild" + Enum.GetName(typeof(OSArchitecture), UserBuildSettings.architecture) + ".app";
        buildPlayerOptions.extraScriptingDefines = new[] { "NETCODE_DEBUG", "UNITY_CLIENT" };
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
    
    [MenuItem("Tools/Builder/Build Standalone MacOS Universal")]
    static void BuildStandAloneOSX_Universal()
    {
        BuildStandAloneOSX(OSArchitecture.x64ARM64, BuildOptions.Development);
    }

    [MenuItem("Tools/Builder/Build Standalone MacOS Intelx64")]
    static void BuildStandAloneOSX_Intelx64()
    {
        BuildStandAloneOSX(OSArchitecture.x64, BuildOptions.Development);
    }
    
    [MenuItem("Tools/Builder/Build Standalone MacOS Silicon")]
    static void BuildStandAloneOSX_Silicon()
    {
        BuildStandAloneOSX(OSArchitecture.ARM64, BuildOptions.Development);
    }

    [MenuItem("Tools/Builder/Build Release Standalone MacOS Universal")]
    static void BuildReleaseStandAloneOSX_Universal()
    {
        BuildStandAloneOSX(OSArchitecture.x64ARM64, BuildOptions.ShowBuiltPlayer);
    }
    
    [MenuItem("Tools/Builder/Build Release Standalone MacOS Intelx64")]
    static void BuildReleaseStandAloneOSX_Intelx64()
    {
        BuildStandAloneOSX(OSArchitecture.x64, BuildOptions.ShowBuiltPlayer);
    }
    
    [MenuItem("Tools/Builder/Build Release Standalone MacOS Silicon")]
    static void BuildReleaseStandAloneOSX_Silicon()
    {
        BuildStandAloneOSX(OSArchitecture.ARM64, BuildOptions.ShowBuiltPlayer);
    }
#endif
    private static BuilderScript instance;
    public static BuilderScript Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("YourClassNameInstance").AddComponent<BuilderScript>();
            }
            return instance;
        }
    }
}