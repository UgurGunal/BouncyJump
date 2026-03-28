using UnityEngine;

/// <summary>
/// Persists music / SFX master volumes in <see cref="PlayerPrefs"/> so the home screen (settings sliders)
/// and <see cref="MusicManager"/> / <see cref="SoundEffectsManager"/> (any scene) stay in sync.
/// </summary>
public static class AudioVolumeSettings
{
    const string MusicKey = "TowerJump_MusicVolume";
    const string SfxKey = "TowerJump_SfxVolume";

    /// <summary>Used when no saved value exists (matches typical UI slider default of 0.5).</summary>
    public const float DefaultVolume = 0.5f;

    public static float GetMusicVolume() => PlayerPrefs.GetFloat(MusicKey, DefaultVolume);

    public static float GetSfxVolume() => PlayerPrefs.GetFloat(SfxKey, DefaultVolume);

    public static void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MusicKey, volume);
        PlayerPrefs.Save();
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetMasterVolume(volume);
    }

    public static void SetSfxVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SfxKey, volume);
        PlayerPrefs.Save();
        if (SoundEffectsManager.Instance != null)
            SoundEffectsManager.Instance.SetMasterVolume(volume);
    }
}
