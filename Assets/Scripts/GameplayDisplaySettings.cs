using UnityEngine;

/// <summary>
/// Persists optional HUD options (e.g. run timer) via <see cref="PlayerPrefs"/>.
/// </summary>
public static class GameplayDisplaySettings
{
    const string ShowRunTimerKey = "TowerJump_ShowRunTimer";

    /// <summary>Default when the player has never changed the setting (timer HUD hidden).</summary>
    public const bool DefaultShowRunTimer = false;

    public static bool GetShowRunTimer() => PlayerPrefs.GetInt(ShowRunTimerKey, DefaultShowRunTimer ? 1 : 0) == 1;

    /// <summary>When false, the in-game run timer HUD is hidden (timer still runs for stats).</summary>
    public static bool ShowRunTimer => GetShowRunTimer();

    public static void SetShowRunTimer(bool show)
    {
        PlayerPrefs.SetInt(ShowRunTimerKey, show ? 1 : 0);
        PlayerPrefs.Save();
    }
}
