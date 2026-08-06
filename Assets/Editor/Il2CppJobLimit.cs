using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Caps IL2CPP parallel compile jobs to reduce RAM/CPU pressure on low-memory machines.
/// UNITY_IL2CPP_JOB_COUNT user env var should also be set; this reinforces it for the Editor process.
/// </summary>
[InitializeOnLoad]
public static class Il2CppJobLimit
{
    const string EnvVar = "UNITY_IL2CPP_JOB_COUNT";
    const string JobCount = "2";

    static Il2CppJobLimit()
    {
        try
        {
            Environment.SetEnvironmentVariable(EnvVar, JobCount, EnvironmentVariableTarget.Process);
            Debug.Log($"[Il2CppJobLimit] {EnvVar}={JobCount} (process)");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Il2CppJobLimit] Could not set {EnvVar}: {ex.Message}");
        }
    }
}
