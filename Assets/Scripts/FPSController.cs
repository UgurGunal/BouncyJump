using UnityEngine;

public class FPSController : MonoBehaviour
{
    [Header("FPS Settings")]
    [SerializeField] private int targetFPS = 60;
    [SerializeField] private bool enableVSync = true;
    [SerializeField] private bool enableOnStart = true;

    void Start()
    {
        if (enableOnStart)
        {
            SetTargetFPS(targetFPS);
        }
    }

    /// <summary>
    /// Sets the target FPS for the game
    /// </summary>
    /// <param name="fps">Target FPS (30, 60, 90, 120, etc.)</param>
    public void SetTargetFPS(int fps)
    {
        targetFPS = Mathf.Clamp(fps, 30, 120); // Clamp between 30 and 120 FPS
        
        if (enableVSync)
        {
            // Use VSync for consistent frame timing
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1; // Let VSync handle it
        }
        else
        {
            // Use target frame rate without VSync
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFPS;
        }
        
        Debug.Log($"FPS Controller: Target FPS set to {targetFPS} (VSync: {enableVSync})");
    }

    /// <summary>
    /// Enables or disables VSync
    /// </summary>
    /// <param name="enable">Whether to enable VSync</param>
    public void SetVSync(bool enable)
    {
        enableVSync = enable;
        SetTargetFPS(targetFPS); // Reapply settings
    }

    /// <summary>
    /// Gets the current target FPS
    /// </summary>
    /// <returns>Current target FPS</returns>
    public int GetTargetFPS()
    {
        return targetFPS;
    }

    /// <summary>
    /// Gets the current actual FPS
    /// </summary>
    /// <returns>Current actual FPS</returns>
    public float GetCurrentFPS()
    {
        return 1f / Time.unscaledDeltaTime;
    }

    /// <summary>
    /// Resets to default settings (60 FPS, VSync enabled)
    /// </summary>
    public void ResetToDefault()
    {
        targetFPS = 60;
        enableVSync = true;
        SetTargetFPS(targetFPS);
    }

    /// <summary>
    /// Optimized settings for mobile (30 FPS, VSync enabled)
    /// </summary>
    public void SetMobileOptimized()
    {
        targetFPS = 30;
        enableVSync = true;
        SetTargetFPS(targetFPS);
    }

    /// <summary>
    /// High performance settings (60 FPS, VSync enabled)
    /// </summary>
    public void SetHighPerformance()
    {
        targetFPS = 60;
        enableVSync = true;
        SetTargetFPS(targetFPS);
    }

    /// <summary>
    /// Ultra performance settings (90 FPS, VSync disabled)
    /// </summary>
    public void SetUltraPerformance()
    {
        targetFPS = 90;
        enableVSync = false;
        SetTargetFPS(targetFPS);
    }
}