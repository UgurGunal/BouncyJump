#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

/// <summary>
/// Apple rejects IPAs when UnityFramework.framework contains a nested Frameworks folder
/// (ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES / CocoaPods). Fix Xcode settings for Cloud Build.
/// </summary>
public static class iOSFixNestedFrameworks
{
    const string ShellPhaseName = "Fix Nested UnityFramework Frameworks";

    [PostProcessBuild(int.MaxValue)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
            return;

        string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        var project = new PBXProject();
        project.ReadFromFile(projectPath);

        string mainTarget = project.GetUnityMainTargetGuid();
        string frameworkTarget = project.GetUnityFrameworkTargetGuid();

        project.SetBuildProperty(frameworkTarget, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
        project.SetBuildProperty(mainTarget, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");

        string script =
            "NESTED=\"${TARGET_BUILD_DIR}/${FRAMEWORKS_FOLDER_PATH}/UnityFramework.framework/Frameworks\"\n" +
            "if [ -d \"$NESTED\" ]; then\n" +
            "  echo \"Removing nested Frameworks from UnityFramework.framework\"\n" +
            "  rm -rf \"$NESTED\"\n" +
            "fi\n";

        project.AddShellScriptBuildPhase(mainTarget, ShellPhaseName, "/bin/sh", script);
        project.WriteToFile(projectPath);

        // Exempt encryption only (HTTPS / OS crypto via Ads & IAP) — skips App Store Connect prompt.
        // ATT: required when App Privacy declares tracking / Unity Ads may use IDFA.
        string plistPath = pathToBuiltProject + "/Info.plist";
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
        plist.root.SetString(
            "NSUserTrackingUsageDescription",
            "This identifier will be used to deliver personalized ads to you.");
        plist.WriteToFile(plistPath);
    }
}
#endif
