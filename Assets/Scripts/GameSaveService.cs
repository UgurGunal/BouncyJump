using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// Central save for gold, diamonds, unlocks, tower records, and settings.
/// Data is stored in persistentDataPath with a SHA-256 integrity check (deters casual editing; not server-grade).
/// Migrates existing PlayerPrefs on first run.
/// </summary>
public static class GameSaveService
{
    const string SaveFileName = "towerjump_save_v1.dat";
    const string LegacyGoldKey = "PlayerGold";
    const string LegacyDiamondsKey = "PlayerDiamonds";
    const string LegacyCurrencyKey = "PlayerCurrency";
    const string LegacyTowerIndexKey = "CurrentTowerIndex";
    const string LegacyBallIndexKey = "CurrentBallIndex";
    const string LegacyTowerVersionKey = "TowerShopSaveVersion";
    const string LegacyBallVersionKey = "BallShopSaveVersion";
    const string LegacyTowerPurchasedPrefix = "TowerPurchased_";
    const string LegacyBallPurchasedPrefix = "BallPurchased_";
    const string LegacyTowerHeightPrefix = "TowerBestHeight_";

    static readonly byte[] HashSecret =
    {
        0x54, 0x6F, 0x77, 0x65, 0x72, 0x4A, 0x75, 0x6D, 0x70, 0x53, 0x61, 0x76, 0x65, 0x4B, 0x65, 0x79,
        0x32, 0x30, 0x32, 0x36
    };

    static GameSaveData _data;
    static bool _loaded;
    static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void LoadBeforeFirstScene()
    {
        EnsureLoaded();
    }

    public static void EnsureLoaded()
    {
        if (_loaded)
            return;

        _loaded = true;

        if (TryLoadFromDisk(out GameSaveData diskData))
        {
            _data = diskData;
            return;
        }

        if (HasLegacyPlayerPrefs())
        {
            _data = MigrateFromPlayerPrefs();
            WriteToDisk();
            ClearLegacyPlayerPrefs();
            return;
        }

        _data = GameSaveData.CreateNew();
        WriteToDisk();
    }

    public static void Save()
    {
        EnsureLoaded();
        WriteToDisk();
    }

    public static void ResetToDefaults()
    {
        _data = GameSaveData.CreateNew();
        _loaded = true;

        if (File.Exists(SavePath))
        {
            try { File.Delete(SavePath); }
            catch (Exception e) { Debug.LogWarning($"GameSaveService: could not delete save file: {e.Message}"); }
        }

        ClearLegacyPlayerPrefs();
        WriteToDisk();
    }

    // --- Currency ---

    public static int GetGold()
    {
        EnsureLoaded();
        return Mathf.Max(0, _data.gold);
    }

    public static int GetDiamonds()
    {
        EnsureLoaded();
        return Mathf.Max(0, _data.diamonds);
    }

    public static void SetGold(int value)
    {
        EnsureLoaded();
        _data.gold = Mathf.Max(0, value);
        Save();
    }

    public static void SetDiamonds(int value)
    {
        EnsureLoaded();
        _data.diamonds = Mathf.Max(0, value);
        Save();
    }

    public static void AddGold(int amount)
    {
        if (amount == 0) return;
        SetGold(GetGold() + amount);
    }

    public static void AddDiamonds(int amount)
    {
        if (amount == 0) return;
        SetDiamonds(GetDiamonds() + amount);
    }

    public static bool TrySpendDiamonds(int amount)
    {
        if (amount <= 0) return true;
        int current = GetDiamonds();
        if (current < amount) return false;
        SetDiamonds(current - amount);
        return true;
    }

    public static bool TrySpendGold(int amount)
    {
        if (amount <= 0) return true;
        int current = GetGold();
        if (current < amount) return false;
        SetGold(current - amount);
        return true;
    }

    // --- Selection ---

    public static int GetCurrentTowerIndex() { EnsureLoaded(); return _data.currentTowerIndex; }
    public static void SetCurrentTowerIndex(int index) { EnsureLoaded(); _data.currentTowerIndex = index; Save(); }

    public static int GetCurrentBallIndex() { EnsureLoaded(); return _data.currentBallIndex; }
    public static void SetCurrentBallIndex(int index) { EnsureLoaded(); _data.currentBallIndex = index; Save(); }

    // --- Shop versions ---

    public static int GetTowerShopSaveVersion() { EnsureLoaded(); return _data.towerShopSaveVersion; }
    public static void SetTowerShopSaveVersion(int version) { EnsureLoaded(); _data.towerShopSaveVersion = version; Save(); }

    public static int GetBallShopSaveVersion() { EnsureLoaded(); return _data.ballShopSaveVersion; }
    public static void SetBallShopSaveVersion(int version) { EnsureLoaded(); _data.ballShopSaveVersion = version; Save(); }

    // --- Purchases ---

    public static bool IsTowerPurchased(int towerIndex)
    {
        if (!IsValidIndex(towerIndex, GameSaveData.MaxTowers)) return false;
        EnsureLoaded();
        return _data.towerPurchased[towerIndex] == 1;
    }

    public static void SetTowerPurchased(int towerIndex, bool purchased)
    {
        if (!IsValidIndex(towerIndex, GameSaveData.MaxTowers)) return;
        EnsureLoaded();
        _data.towerPurchased[towerIndex] = purchased ? 1 : 0;
        Save();
    }

    public static void ClearTowerPurchases()
    {
        EnsureLoaded();
        Array.Clear(_data.towerPurchased, 0, _data.towerPurchased.Length);
        Save();
    }

    public static bool IsBallPurchased(int ballIndex)
    {
        if (!IsValidIndex(ballIndex, GameSaveData.MaxBalls)) return false;
        EnsureLoaded();
        return _data.ballPurchased[ballIndex] == 1;
    }

    public static void SetBallPurchased(int ballIndex, bool purchased)
    {
        if (!IsValidIndex(ballIndex, GameSaveData.MaxBalls)) return;
        EnsureLoaded();
        _data.ballPurchased[ballIndex] = purchased ? 1 : 0;
        Save();
    }

    public static void ClearBallPurchases()
    {
        EnsureLoaded();
        Array.Clear(_data.ballPurchased, 0, _data.ballPurchased.Length);
        Save();
    }

    // --- Tower best heights ---

    public static float GetTowerBestRawHeight(int towerIndex)
    {
        if (!IsValidIndex(towerIndex, GameSaveData.MaxTowers)) return 0f;
        EnsureLoaded();
        return Mathf.Max(0f, _data.towerBestHeights[towerIndex]);
    }

    public static bool TryRecordTowerBestHeight(int towerIndex, float sessionRawHeight)
    {
        if (!IsValidIndex(towerIndex, GameSaveData.MaxTowers)) return false;
        EnsureLoaded();
        float best = _data.towerBestHeights[towerIndex];
        if (sessionRawHeight <= best) return false;
        _data.towerBestHeights[towerIndex] = sessionRawHeight;
        Save();
        return true;
    }

    // --- Settings ---

    public static float GetMusicVolume() { EnsureLoaded(); return Mathf.Clamp01(_data.musicVolume); }
    public static float GetSfxVolume() { EnsureLoaded(); return Mathf.Clamp01(_data.sfxVolume); }
    public static bool GetShowRunTimer() { EnsureLoaded(); return _data.showRunTimer == 1; }

    public static void SetMusicVolume(float volume)
    {
        EnsureLoaded();
        _data.musicVolume = Mathf.Clamp01(volume);
        Save();
    }

    public static void SetSfxVolume(float volume)
    {
        EnsureLoaded();
        _data.sfxVolume = Mathf.Clamp01(volume);
        Save();
    }

    public static void SetShowRunTimer(bool show)
    {
        EnsureLoaded();
        _data.showRunTimer = show ? 1 : 0;
        Save();
    }

    // --- Disk I/O ---

    static bool TryLoadFromDisk(out GameSaveData data)
    {
        data = null;
        if (!File.Exists(SavePath))
            return false;

        try
        {
            string envelopeJson = File.ReadAllText(SavePath, Encoding.UTF8);
            SaveFileEnvelope envelope = JsonUtility.FromJson<SaveFileEnvelope>(envelopeJson);
            if (envelope == null || string.IsNullOrEmpty(envelope.payload) || string.IsNullOrEmpty(envelope.hash))
                return false;

            if (!VerifyHash(envelope.payload, envelope.hash))
            {
                Debug.LogWarning("GameSaveService: save file failed integrity check (possible tampering).");
                return false;
            }

            data = JsonUtility.FromJson<GameSaveData>(envelope.payload);
            if (data == null)
                return false;

            EnsureArraySizes(data);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"GameSaveService: failed to load save: {e.Message}");
            return false;
        }
    }

    static void WriteToDisk()
    {
        if (_data == null)
            _data = GameSaveData.CreateNew();

        EnsureArraySizes(_data);

        try
        {
            string payload = JsonUtility.ToJson(_data);
            var envelope = new SaveFileEnvelope
            {
                payload = payload,
                hash = ComputeHashHex(payload)
            };

            string directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string tempPath = SavePath + ".tmp";
            File.WriteAllText(tempPath, JsonUtility.ToJson(envelope), Encoding.UTF8);
            if (File.Exists(SavePath))
                File.Delete(SavePath);
            File.Move(tempPath, SavePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"GameSaveService: failed to write save: {e.Message}");
        }
    }

    static GameSaveData MigrateFromPlayerPrefs()
    {
        var data = GameSaveData.CreateNew();
        data.gold = PlayerPrefs.GetInt(LegacyGoldKey, 0);
        data.diamonds = PlayerPrefs.GetInt(LegacyDiamondsKey, 0);
        data.legacyCurrency = PlayerPrefs.GetInt(LegacyCurrencyKey, 0);
        data.currentTowerIndex = PlayerPrefs.GetInt(LegacyTowerIndexKey, 0);
        data.currentBallIndex = PlayerPrefs.GetInt(LegacyBallIndexKey, 0);
        data.towerShopSaveVersion = PlayerPrefs.GetInt(LegacyTowerVersionKey, 0);
        data.ballShopSaveVersion = PlayerPrefs.GetInt(LegacyBallVersionKey, 0);

        for (int i = 0; i < GameSaveData.MaxTowers; i++)
            data.towerPurchased[i] = PlayerPrefs.GetInt($"{LegacyTowerPurchasedPrefix}{i}", 0);

        for (int i = 0; i < GameSaveData.MaxBalls; i++)
            data.ballPurchased[i] = PlayerPrefs.GetInt($"{LegacyBallPurchasedPrefix}{i}", 0);

        for (int i = 0; i < GameSaveData.MaxTowers; i++)
            data.towerBestHeights[i] = PlayerPrefs.GetFloat($"{LegacyTowerHeightPrefix}{i}", 0f);

        data.musicVolume = PlayerPrefs.GetFloat("TowerJump_MusicVolume", 0.5f);
        data.sfxVolume = PlayerPrefs.GetFloat("TowerJump_SfxVolume", 0.5f);
        data.showRunTimer = PlayerPrefs.GetInt("TowerJump_ShowRunTimer", GameplayDisplaySettings.DefaultShowRunTimer ? 1 : 0);

        return data;
    }

    static bool HasLegacyPlayerPrefs()
    {
        return PlayerPrefs.HasKey(LegacyGoldKey)
            || PlayerPrefs.HasKey(LegacyDiamondsKey)
            || PlayerPrefs.HasKey(LegacyTowerIndexKey)
            || PlayerPrefs.HasKey($"{LegacyTowerPurchasedPrefix}0");
    }

    static void ClearLegacyPlayerPrefs()
    {
        PlayerPrefs.DeleteKey(LegacyGoldKey);
        PlayerPrefs.DeleteKey(LegacyDiamondsKey);
        PlayerPrefs.DeleteKey(LegacyCurrencyKey);
        PlayerPrefs.DeleteKey(LegacyTowerIndexKey);
        PlayerPrefs.DeleteKey(LegacyBallIndexKey);
        PlayerPrefs.DeleteKey("SelectedTower");
        PlayerPrefs.DeleteKey(LegacyTowerVersionKey);
        PlayerPrefs.DeleteKey(LegacyBallVersionKey);

        for (int i = 0; i < GameSaveData.MaxTowers; i++)
        {
            PlayerPrefs.DeleteKey($"{LegacyTowerPurchasedPrefix}{i}");
            PlayerPrefs.DeleteKey($"{LegacyTowerHeightPrefix}{i}");
        }

        for (int i = 0; i < GameSaveData.MaxBalls; i++)
            PlayerPrefs.DeleteKey($"{LegacyBallPurchasedPrefix}{i}");

        PlayerPrefs.Save();
    }

    static string ComputeHashHex(string payload)
    {
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
        byte[] combined = new byte[payloadBytes.Length + HashSecret.Length];
        Buffer.BlockCopy(payloadBytes, 0, combined, 0, payloadBytes.Length);
        Buffer.BlockCopy(HashSecret, 0, combined, payloadBytes.Length, HashSecret.Length);

        using (var sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(combined);
            var sb = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }
    }

    static bool VerifyHash(string payload, string expectedHex)
    {
        return string.Equals(ComputeHashHex(payload), expectedHex, StringComparison.OrdinalIgnoreCase);
    }

    static void EnsureArraySizes(GameSaveData data)
    {
        if (data.towerPurchased == null || data.towerPurchased.Length != GameSaveData.MaxTowers)
            data.towerPurchased = new int[GameSaveData.MaxTowers];
        if (data.ballPurchased == null || data.ballPurchased.Length != GameSaveData.MaxBalls)
            data.ballPurchased = new int[GameSaveData.MaxBalls];
        if (data.towerBestHeights == null || data.towerBestHeights.Length != GameSaveData.MaxTowers)
            data.towerBestHeights = new float[GameSaveData.MaxTowers];
    }

    static bool IsValidIndex(int index, int max) => index >= 0 && index < max;

    [Serializable]
    class SaveFileEnvelope
    {
        public string payload;
        public string hash;
    }
}
