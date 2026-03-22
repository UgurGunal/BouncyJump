using UnityEngine;

/// <summary>
/// Persists the best (highest) height reached per tower index.
/// Uses the same world-space Y value as <see cref="PointsManager.HighestHeightReached"/>.
/// Display values use the same *5 multiplier as <see cref="GameEndPanelUI"/> / <see cref="GameStatsUI"/>.
/// </summary>
public static class TowerHeightHighScore
{
    private const string KeyPrefix = "TowerBestHeight_";

    /// <summary>Returns stored best raw height (world Y) for this tower, or 0 if none.</summary>
    public static float GetBestRawHeight(int towerIndex)
    {
        if (towerIndex < 0) return 0f;
        return PlayerPrefs.GetFloat($"{KeyPrefix}{towerIndex}", 0f);
    }

    /// <summary>Same display number as session height in UI: Mathf.RoundToInt(rawHeight * 5).</summary>
    public static int GetBestDisplayHeight(int towerIndex)
    {
        return Mathf.RoundToInt(GetBestRawHeight(towerIndex) * 5f);
    }

    /// <summary>If session height is higher than stored best for this tower, saves and returns true.</summary>
    public static bool TryRecordHeight(int towerIndex, float sessionRawHeight)
    {
        if (towerIndex < 0) return false;
        float best = GetBestRawHeight(towerIndex);
        if (sessionRawHeight <= best) return false;

        PlayerPrefs.SetFloat($"{KeyPrefix}{towerIndex}", sessionRawHeight);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>Reads <c>CurrentTowerIndex</c> from PlayerPrefs (same key as TowerManager).</summary>
    public static int GetCurrentTowerIndexFromSave()
    {
        return PlayerPrefs.GetInt("CurrentTowerIndex", 0);
    }
}
