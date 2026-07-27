using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds an Android APK from the Editor menu or via a trigger file for automation.
/// Drop an empty file named BuildAndroid.now at the project root to start a build
/// while the Editor is open.
/// </summary>
[InitializeOnLoad]
public static class AndroidBuilder
{
    const string TriggerFileName = "BuildAndroid.now";
    const string StatusFileName = "BuildAndroid.status";
    const string DefaultApkRelativePath = "Builds/Android/bouncyjump.apk";

    static bool _isBuilding;

    static AndroidBuilder()
    {
        EditorApplication.update += PollTrigger;
    }

    static void PollTrigger()
    {
        if (_isBuilding || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string triggerPath = Path.Combine(projectRoot, TriggerFileName);
        if (!File.Exists(triggerPath))
            return;

        try
        {
            File.Delete(triggerPath);
        }
        catch
        {
            return;
        }

        EditorApplication.delayCall += () => BuildApk();
    }

    [MenuItem("Build/Android APK")]
    public static void BuildApkMenu()
    {
        BuildApk();
    }

    public static void BuildApk()
    {
        if (_isBuilding)
        {
            Debug.LogWarning("[AndroidBuilder] Build already in progress.");
            return;
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string apkPath = Path.Combine(projectRoot, DefaultApkRelativePath);
        string statusPath = Path.Combine(projectRoot, StatusFileName);

        WriteStatus(statusPath, "started");

        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            WriteStatus(statusPath, "failed: no enabled scenes in Build Settings");
            Debug.LogError("[AndroidBuilder] No enabled scenes in Build Settings.");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(apkPath));

        // Billing Client 9.x requires minSdk 23. Force it here so an open Editor
        // with a stale Player Settings value (e.g. 22) cannot break the build.
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.rugustudios.bouncyjump");

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = false;

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        _isBuilding = true;
        Debug.Log($"[AndroidBuilder] Building APK → {apkPath} (minSdk={(int)PlayerSettings.Android.minSdkVersion})");

        try
        {
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                WriteStatus(statusPath, $"succeeded: {apkPath}");
                Debug.Log($"[AndroidBuilder] Succeeded in {summary.totalTime}. Size={summary.totalSize} bytes. Output={apkPath}");
            }
            else
            {
                WriteStatus(statusPath, $"failed: {summary.result}");
                Debug.LogError($"[AndroidBuilder] Failed: {summary.result}");
            }
        }
        catch (System.Exception ex)
        {
            WriteStatus(statusPath, $"failed: {ex.Message}");
            Debug.LogError($"[AndroidBuilder] Exception: {ex}");
        }
        finally
        {
            _isBuilding = false;
        }
    }

    /// <summary>CLI entry: Unity.exe -batchmode -quit -projectPath ... -executeMethod AndroidBuilder.BuildApkBatch</summary>
    public static void BuildApkBatch()
    {
        BuildApk();
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string statusPath = Path.Combine(projectRoot, StatusFileName);
        int code = 1;
        if (File.Exists(statusPath) && File.ReadAllText(statusPath).StartsWith("succeeded"))
            code = 0;
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }

    static string[] GetEnabledScenes()
    {
        var scenes = EditorBuildSettings.scenes;
        var enabled = new System.Collections.Generic.List<string>(scenes.Length);
        foreach (var scene in scenes)
        {
            if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                enabled.Add(scene.path);
        }
        return enabled.ToArray();
    }

    static void WriteStatus(string path, string text)
    {
        try
        {
            File.WriteAllText(path, text);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[AndroidBuilder] Could not write status file: {ex.Message}");
        }
    }
}
