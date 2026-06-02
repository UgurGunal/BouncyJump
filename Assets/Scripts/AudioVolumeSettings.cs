using UnityEngine;

/// <summary>
/// Persists music / SFX master volumes via <see cref="GameSaveService"/>.
/// </summary>
public static class AudioVolumeSettings
{
    /// <summary>Used when no saved value exists (matches typical UI slider default of 0.5).</summary>
    public const float DefaultVolume = 0.5f;

    public static float GetMusicVolume() => GameSaveService.GetMusicVolume();

    public static float GetSfxVolume() => GameSaveService.GetSfxVolume();

    public static void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        GameSaveService.SetMusicVolume(volume);
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetMasterVolume(volume);
    }

    public static void SetSfxVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        GameSaveService.SetSfxVolume(volume);
        if (SoundEffectsManager.Instance != null)
            SoundEffectsManager.Instance.SetMasterVolume(volume);
    }
}
