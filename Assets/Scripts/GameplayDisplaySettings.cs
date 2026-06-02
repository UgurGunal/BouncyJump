using UnityEngine;

/// <summary>
/// Persists optional HUD options (e.g. run timer) via <see cref="GameSaveService"/>.
/// </summary>
public static class GameplayDisplaySettings
{
    /// <summary>Default when the player has never changed the setting (timer HUD hidden).</summary>
    public const bool DefaultShowRunTimer = false;

    public static bool GetShowRunTimer() => GameSaveService.GetShowRunTimer();

    /// <summary>When false, the in-game run timer HUD is hidden (timer still runs for stats).</summary>
    public static bool ShowRunTimer => GetShowRunTimer();

    public static void SetShowRunTimer(bool show)
    {
        GameSaveService.SetShowRunTimer(show);
    }
}
