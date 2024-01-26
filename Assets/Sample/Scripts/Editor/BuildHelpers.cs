using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

/// <summary>
/// Utility menus to easily create our builds for our playtests. If you're just exploring this project, you shouldn't need those. They are mostly to make
/// multiplatform build creation easier and is meant for internal usage.
/// </summary>
internal static class BuildHelpers
{
    const string k_MenuRoot = "Boss Room/Playtest Builds/";
    const string k_Build = k_MenuRoot + "Build";
    const string k_DeleteBuilds = k_MenuRoot + "Delete All Builds (keeps cache)";
    const string k_AllToggleName = k_MenuRoot + "Toggle All";
    const string k_MobileToggleName = k_MenuRoot + "Toggle Mobile";
    const string k_IOSToggleName = k_MenuRoot + "Toggle iOS";
    const string k_AndroidToggleName = k_MenuRoot + "Toggle Android";
    const string k_DesktopToggleName = k_MenuRoot + "Toggle Desktop";
    const string k_MacOSToggleName = k_MenuRoot + "Toggle MacOS";
    const string k_WindowsToggleName = k_MenuRoot + "Toggle Windows";
    const string k_DisableProjectIDToggleName = k_MenuRoot + "Skip Project ID Check"; // double negative in the name since menu is unchecked by default
    const string k_SkipAutoDeleteToggleName = k_MenuRoot + "Skip Auto Delete Builds";

    const int k_MenuGroupingBuild = 0; // to add separator in menus
    const int k_MenuGroupingPlatforms = 11;
    const int k_MenuGroupingOtherToggles = 22;

    static BuildTarget s_CurrentEditorBuildTarget;
    static BuildTargetGroup s_CurrentEditorBuildTargetGroup;
    static int s_NbBuildsDone;

    static string BuildPathRootDirectory => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", "Playtest");
    static string BuildPathDirectory(string platformName) => Path.Combine(BuildPathRootDirectory, platformName);
    public static string BuildPath(string platformName) => Path.Combine(BuildPathDirectory(platformName), "BossRoomPlaytest");

    [MenuItem(k_Build, false, k_MenuGroupingBuild)]
    static async void Build()
    {
        s_NbBuildsDone = 0;
        bool buildiOS = Menu.GetChecked(k_IOSToggleName);
        bool buildAndroid = Menu.GetChecked(k_AndroidToggleName);
        bool buildMacOS = Menu.GetChecked(k_MacOSToggleName);
        bool buildWindows = Menu.GetChecked(k_WindowsToggleName);

        bool skipAutoDelete = Menu.GetChecked(k_SkipAutoDeleteToggleName);

        Debug.Log($"Starting build: buildiOS?:{buildiOS} buildAndroid?:{buildAndroid} buildMacOS?:{buildMacOS} buildWindows?:{buildWindows}");
        if (string.IsNullOrEmpty(CloudProjectSettings.projectId) && !Menu.GetChecked(k_DisableProjectIDToggleName))
        {
            string errorMessage = $"Project ID was supposed to be setup and wasn't, make sure to set it up or disable project ID check with the [{k_DisableProjectIDToggleName}] menu";
            EditorUtility.DisplayDialog("Error Custom Build", errorMessage, "ok");
            throw new Exception(errorMessage);
        }

        SaveCurrentBuildTarget();

        try
        {
            // deleting so we don't end up testing on outdated builds if there's a build failure
            if (!skipAutoDelete) DeleteBuilds();

            if (buildiOS) await BuildPlayerUtilityAsync(BuildTarget.iOS, "", true);
            if (buildAndroid) await BuildPlayerUtilityAsync(BuildTarget.Android, ".apk", true); // there's the possibility of an error where it

            // complains about NDK missing. Building manually on android then trying again seems to work? Can't find anything on this.
            if (buildMacOS) await BuildPlayerUtilityAsync(BuildTarget.StandaloneOSX, ".app", true);
            if (buildWindows) await BuildPlayerUtilityAsync(BuildTarget.StandaloneWindows64, ".exe", true);
        }
        catch
        {
            EditorUtility.DisplayDialog("Exception while building", "See console for details", "ok");
            throw;
        }
        finally
        {
            Debug.Log($"Count builds done: {s_NbBuildsDone}");
            RestoreBuildTarget();
        }
    }

    [MenuItem(k_Build, true)]
    static bool CanBuild()
    {
        return Menu.GetChecked(k_IOSToggleName) ||
            Menu.GetChecked(k_AndroidToggleName) ||
            Menu.GetChecked(k_MacOSToggleName) ||
            Menu.GetChecked(k_WindowsToggleName);
    }

    static void RestoreBuildTarget()
    {
        Debug.Log($"restoring editor to initial build target {s_CurrentEditorBuildTarget}");
        EditorUserBuildSettings.SwitchActiveBuildTarget(s_CurrentEditorBuildTargetGroup, s_CurrentEditorBuildTarget);
    }

    static void SaveCurrentBuildTarget()
    {
        s_CurrentEditorBuildTarget = EditorUserBuildSettings.activeBuildTarget;
        s_CurrentEditorBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
    }

    [MenuItem(k_AllToggleName, false, k_MenuGroupingPlatforms)]
    static void ToggleAll()
    {
        var newValue = ToggleMenu(k_AllToggleName);
        ToggleMenu(k_DesktopToggleName, newValue);
        ToggleMenu(k_MacOSToggleName, newValue);
        ToggleMenu(k_WindowsToggleName, newValue);
        ToggleMenu(k_MobileToggleName, newValue);
        ToggleMenu(k_IOSToggleName, newValue);
        ToggleMenu(k_AndroidToggleName, newValue);
    }

    [MenuItem(k_MobileToggleName, false, k_MenuGroupingPlatforms)]
    static void ToggleMobile()
    {
        var newValue = ToggleMenu(k_MobileToggleName);
        ToggleMenu(k_IOSToggleName, newValue);
        ToggleMenu(k_AndroidToggleName, newValue);
    }

    [MenuItem(k_IOSToggleName, false, k_MenuGroupingPlatforms)]
    static void ToggleiOS()
    {
        ToggleMenu(k_IOSToggleName);
    }

    [MenuItem(k_AndroidToggleName, false, k_MenuGroupingPlatforms)]
    static void ToggleAndroid()
    {
        ToggleMenu(k_AndroidToggleName);
    }

    [MenuItem(k_DesktopToggleName, false, k_MenuGroupingPlatforms)]
    static void ToggleDesktop()
    {
        var newValue = ToggleMenu(k_DesktopToggleName);
        ToggleMenu(k_MacOSToggleName, newValue);
        ToggleMenu(k_WindowsToggleName, newValue);
    }

    [MenuItem(k_MacOSToggleName, false, k_MenuGroupingPlatforms)]
    static void ToggleMacOS()
    {
        ToggleMenu(k_MacOSToggleName);
    }

    [MenuItem(k_WindowsToggleName, false, k_MenuGroupingPlatforms)]
    static void ToggleWindows()
    {
        ToggleMenu(k_WindowsToggleName);
    }

    [MenuItem(k_DisableProjectIDToggleName, false, k_MenuGroupingOtherToggles)]
    static void ToggleProjectID()
    {
        ToggleMenu(k_DisableProjectIDToggleName);
    }

    [MenuItem(k_SkipAutoDeleteToggleName, false, k_MenuGroupingOtherToggles)]
    static void ToggleAutoDelete()
    {
        ToggleMenu(k_SkipAutoDeleteToggleName);
    }

    static bool ToggleMenu(string menuName, bool? valueToSet = null)
    {
        bool toSet = !Menu.GetChecked(menuName);
        if (valueToSet != null)
        {
            toSet = valueToSet.Value;
        }

        Menu.SetChecked(menuName, toSet);
        return toSet;
    }

    static async Task BuildPlayerUtilityAsync(BuildTarget buildTarget = BuildTarget.NoTarget, string buildPathExtension = null, bool buildDebug = false)
    {
        s_NbBuildsDone++;
        Debug.Log($"Starting build for {buildTarget.ToString()}");

        await Task.Delay(100); // skipping some time to make sure debug logs are flushed before we build

        var buildPathToUse = BuildPath(buildTarget.ToString());
        buildPathToUse += buildPathExtension;

        var buildPlayerOptions = new BuildPlayerOptions();

        List<string> scenesToInclude = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenesToInclude.Add(scene.path);
            }
        }

        buildPlayerOptions.scenes = scenesToInclude.ToArray();
        buildPlayerOptions.locationPathName = buildPathToUse;
        buildPlayerOptions.target = buildTarget;
        var buildOptions = BuildOptions.None;
        if (buildDebug)
        {
            buildOptions |= BuildOptions.Development;
        }

        buildOptions |= BuildOptions.StrictMode;
        buildPlayerOptions.options = buildOptions;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {summary.totalSize} bytes at {summary.outputPath}");
        }
        else
        {
            string debugString = buildDebug ? "debug" : "release";
            throw new Exception($"Build failed for {debugString}:{buildTarget}! {report.summary.totalErrors} errors");
        }
    }

    [MenuItem(k_DeleteBuilds, false, k_MenuGroupingBuild)]
    public static void DeleteBuilds()
    {
        if (Directory.Exists(BuildPathRootDirectory))
        {
            Directory.Delete(BuildPathRootDirectory, recursive: true);
            Debug.Log($"deleted {BuildPathRootDirectory}");
        }
        else
        {
            Debug.Log($"Build directory does not exist ({BuildPathRootDirectory}). No cleanup to do");
        }
    }
}
