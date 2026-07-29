using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds Android APK/AAB from the Editor menu or via a trigger file for automation.
/// Drop an empty file named BuildAndroid.now at the project root to start an APK build
/// while the Editor is open. Use BuildAndroidAab.now for an App Bundle (Play Console).
/// </summary>
[InitializeOnLoad]
public static class AndroidBuilder
{
    const string TriggerFileName = "BuildAndroid.now";
    const string AabTriggerFileName = "BuildAndroidAab.now";
    const string StatusFileName = "BuildAndroid.status";
    const string DefaultApkRelativePath = "Builds/Android/bouncyjump.apk";
    const string DefaultAabRelativePath = "Builds/Android/bouncyjump.aab";

    // Google Play requires target API 35+ for current uploads.
    const AndroidSdkVersions RequiredTargetSdk = (AndroidSdkVersions)35;

    static bool _isBuilding;
    static bool _triggerQueued;

    static AndroidBuilder()
    {
        EditorApplication.update += PollTrigger;
    }

    static void PollTrigger()
    {
        if (_isBuilding || _triggerQueued || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string aabTriggerPath = Path.Combine(Application.dataPath, "Editor", AabTriggerFileName);
        if (!File.Exists(aabTriggerPath))
            aabTriggerPath = Path.Combine(projectRoot, AabTriggerFileName);
        string triggerPath = Path.Combine(projectRoot, TriggerFileName);
        string statusPath = Path.Combine(projectRoot, StatusFileName);

        if (File.Exists(aabTriggerPath))
        {
            _triggerQueued = true;
            string processingPath = aabTriggerPath + ".processing";
            try
            {
                if (File.Exists(processingPath))
                    File.Delete(processingPath);
                File.Move(aabTriggerPath, processingPath);
                WriteStatus(statusPath, "queued-aab");
                Debug.Log("[AndroidBuilder] Queued AAB build from trigger.");
            }
            catch (System.Exception ex)
            {
                _triggerQueued = false;
                Debug.LogWarning($"[AndroidBuilder] Could not claim AAB trigger: {ex.Message}");
                return;
            }

            EditorApplication.delayCall += () =>
            {
                _triggerQueued = false;
                if (_isBuilding)
                    return;
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    // Put the trigger back so we retry on a later update tick.
                    try
                    {
                        if (File.Exists(processingPath) && !File.Exists(aabTriggerPath))
                            File.Move(processingPath, aabTriggerPath);
                    }
                    catch { /* ignore */ }
                    WriteStatus(statusPath, "waiting-editor-idle");
                    return;
                }

                try { if (File.Exists(processingPath)) File.Delete(processingPath); }
                catch { /* ignore */ }
                BuildAab();
            };
            return;
        }

        if (!File.Exists(triggerPath))
            return;

        _triggerQueued = true;
        string apkProcessingPath = triggerPath + ".processing";
        try
        {
            if (File.Exists(apkProcessingPath))
                File.Delete(apkProcessingPath);
            File.Move(triggerPath, apkProcessingPath);
            WriteStatus(statusPath, "queued-apk");
        }
        catch (System.Exception ex)
        {
            _triggerQueued = false;
            Debug.LogWarning($"[AndroidBuilder] Could not claim APK trigger: {ex.Message}");
            return;
        }

        EditorApplication.delayCall += () =>
        {
            _triggerQueued = false;
            if (_isBuilding)
                return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                try
                {
                    if (File.Exists(apkProcessingPath) && !File.Exists(triggerPath))
                        File.Move(apkProcessingPath, triggerPath);
                }
                catch { /* ignore */ }
                WriteStatus(statusPath, "waiting-editor-idle");
                return;
            }

            try { if (File.Exists(apkProcessingPath)) File.Delete(apkProcessingPath); }
            catch { /* ignore */ }
            BuildApk();
        };
    }

    [MenuItem("Build/Android APK")]
    public static void BuildApkMenu()
    {
        BuildApk();
    }

    [MenuItem("Build/Android App Bundle (AAB)")]
    public static void BuildAabMenu()
    {
        BuildAab();
    }

    public static void BuildApk()
    {
        BuildAndroid(appBundle: false, DefaultApkRelativePath);
    }

    public static void BuildAab()
    {
        BuildAndroid(appBundle: true, DefaultAabRelativePath);
    }

    static void BuildAndroid(bool appBundle, string relativeOutputPath)
    {
        if (_isBuilding)
        {
            Debug.LogWarning("[AndroidBuilder] Build already in progress.");
            return;
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputPath = Path.Combine(projectRoot, relativeOutputPath);
        string statusPath = Path.Combine(projectRoot, StatusFileName);

        WriteStatus(statusPath, "started");

        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            WriteStatus(statusPath, "failed: no enabled scenes in Build Settings");
            Debug.LogError("[AndroidBuilder] No enabled scenes in Build Settings.");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        if (!ApplyAndroidPlayerSettings(appBundle))
        {
            WriteStatus(statusPath, "failed: missing keystore password (set AndroidKeystore.local or ANDROID_KEYSTORE_PASS)");
            return;
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        _isBuilding = true;
        string kind = appBundle ? "AAB" : "APK";
        Debug.Log(
            $"[AndroidBuilder] Building {kind} → {outputPath} " +
            $"(minSdk={(int)PlayerSettings.Android.minSdkVersion}, " +
            $"targetSdk={(int)PlayerSettings.Android.targetSdkVersion}, " +
            $"versionCode={PlayerSettings.Android.bundleVersionCode}, " +
            $"symbols={EditorUserBuildSettings.androidCreateSymbols})");

        try
        {
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                WriteStatus(statusPath, $"succeeded: {outputPath}");
                Debug.Log($"[AndroidBuilder] Succeeded in {summary.totalTime}. Size={summary.totalSize} bytes. Output={outputPath}");
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

    static bool ApplyAndroidPlayerSettings(bool appBundle)
    {
        // Billing Client 9.x requires minSdk 23. Force it here so an open Editor
        // with a stale Player Settings value (e.g. 22) cannot break the build.
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.Android.targetSdkVersion = RequiredTargetSdk;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.rugustudios.bouncyjump");

        if (!ApplyKeystorePasswordsFromEnvironment() && PlayerSettings.Android.useCustomKeystore)
        {
            Debug.LogError("[AndroidBuilder] Aborting: custom keystore is enabled but no password was provided.");
            return false;
        }

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = appBundle;

        // Public symbols.zip for Play Console native crash symbolication (IL2CPP).
        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
        return true;
    }

    /// <summary>
    /// Automated builds cannot prompt for keystore passwords. Provide them via:
    ///   1) Env ANDROID_KEYSTORE_PASS (+ optional ANDROID_KEYALIAS_PASS), or
    ///   2) Project-root file AndroidKeystore.local with lines:
    ///        keystorePass=...
    ///        keyaliasPass=...
    /// Or enter them once in Player Settings → Publishing Settings in the Editor UI.
    /// Returns true when passwords were applied from env/file.
    /// </summary>
    static bool ApplyKeystorePasswordsFromEnvironment()
    {
        string storePass = System.Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
        string aliasPass = System.Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_PASS");

        if (string.IsNullOrEmpty(storePass))
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string localPath = Path.Combine(projectRoot, "AndroidKeystore.local");
            if (!File.Exists(localPath))
            {
                Debug.LogWarning($"[AndroidBuilder] AndroidKeystore.local not found at {localPath}");
            }
            else
            {
                Debug.Log("[AndroidBuilder] Reading AndroidKeystore.local");
                foreach (string rawLine in File.ReadAllLines(localPath, System.Text.Encoding.UTF8))
                {
                    string line = rawLine.Trim().TrimStart('\uFEFF');
                    if (line.Length == 0 || line.StartsWith("#"))
                        continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                        continue;
                    string key = line.Substring(0, eq).Trim().TrimStart('\uFEFF');
                    string value = line.Substring(eq + 1).Trim();
                    if (key.Equals("keystorePass", System.StringComparison.OrdinalIgnoreCase))
                        storePass = value;
                    else if (key.Equals("keyaliasPass", System.StringComparison.OrdinalIgnoreCase))
                        aliasPass = value;
                }
                Debug.Log(
                    "[AndroidBuilder] AndroidKeystore.local parse: " +
                    $"keystorePass={(string.IsNullOrEmpty(storePass) ? "missing" : "set")}, " +
                    $"keyaliasPass={(string.IsNullOrEmpty(aliasPass) ? "missing" : "set")}");
            }
        }

        if (string.IsNullOrEmpty(storePass))
        {
            Debug.LogWarning(
                "[AndroidBuilder] No keystore password found. Set ANDROID_KEYSTORE_PASS " +
                "or create AndroidKeystore.local, or enter passwords in Publishing Settings.");
            return false;
        }

        PlayerSettings.Android.keystorePass = storePass;
        PlayerSettings.Android.keyaliasPass = string.IsNullOrEmpty(aliasPass) ? storePass : aliasPass;
        Debug.Log("[AndroidBuilder] Applied keystore passwords for signing.");
        return true;
    }

    /// <summary>CLI entry: Unity.exe -batchmode -quit -projectPath ... -executeMethod AndroidBuilder.BuildApkBatch</summary>
    public static void BuildApkBatch()
    {
        BuildApk();
        ExitBatchWithStatus();
    }

    /// <summary>CLI entry: Unity.exe -batchmode -quit -projectPath ... -executeMethod AndroidBuilder.BuildAabBatch</summary>
    public static void BuildAabBatch()
    {
        BuildAab();
        ExitBatchWithStatus();
    }

    static void ExitBatchWithStatus()
    {
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
