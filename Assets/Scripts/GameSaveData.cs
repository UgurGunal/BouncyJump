using System;

/// <summary>
/// Serializable player progress. Stored on disk via <see cref="GameSaveService"/> (not plain PlayerPrefs).
/// </summary>
[Serializable]
public class GameSaveData
{
    public const int FormatVersion = 1;
    public const int MaxTowers = 64;
    public const int MaxBalls = 64;

    public int formatVersion = FormatVersion;
    public int gold;
    public int diamonds;
    public int legacyCurrency;

    public int currentTowerIndex;
    public int currentBallIndex;
    public int towerShopSaveVersion;
    public int ballShopSaveVersion;

    /// <summary>1 = purchased (or default-unlocked tracked at runtime).</summary>
    public int[] towerPurchased = new int[MaxTowers];
    public int[] ballPurchased = new int[MaxBalls];

    /// <summary>Best raw world Y per tower index.</summary>
    public float[] towerBestHeights = new float[MaxTowers];

    public float musicVolume = 0.5f;
    public float sfxVolume = 0.5f;
    public int showRunTimer;

    /// <summary>1 = first-run tutorial finished. Missing on older saves = not completed.</summary>
    public int tutorialCompleted;

    public static GameSaveData CreateNew()
    {
        return new GameSaveData
        {
            formatVersion = FormatVersion,
            musicVolume = 0.5f,
            sfxVolume = 0.5f,
            showRunTimer = GameplayDisplaySettings.DefaultShowRunTimer ? 1 : 0
        };
    }
}
