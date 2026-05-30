using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all sound effects in the game.
/// This is a singleton that persists across all scenes (should be placed in the persistent scene).
/// Sound effects are shared across all scenes.
/// </summary>
public class SoundEffectsManager : MonoBehaviour
{
    public static SoundEffectsManager Instance { get; private set; }

    [Header("Audio Source Settings")]
    [Tooltip("Number of AudioSource components for playing multiple sounds simultaneously")]
    public int audioSourcePoolSize = 10;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 0.5f;
    
    [Header("Core Sound Effects (always used)")]
    [Tooltip("Wall bounce sound effect (name is always 'wall').")]
    public AudioClip wallClip;
    [Tooltip("Coin pickup sound effect (name is always 'coin').")]
    public AudioClip coinClip;
    [Tooltip("Bouncy platform sound effect (name is always 'bouncyPlatform').")]
    public AudioClip bouncyPlatformClip;
    [Tooltip("Anvil underside collision sound effect (name is always 'anvil').")]
    public AudioClip anvilClip;
    [Tooltip("Chest collision sound effect (name is always 'chest').")]
    public AudioClip chestClip;
    [Tooltip("Diamond/gem pickup (name is always 'diamond').")]
    public AudioClip diamondClip;
    [Tooltip("Powerup pickup (name is always 'powerup').")]
    public AudioClip powerupClip;
    [Tooltip("Home menu tower carousel left/right swipe (name is always 'homeTowerSwipe').")]
    public AudioClip homeTowerSwipeClip;
    [Tooltip("Shop purchase for buy gold and buy diamond packs (name is always 'shopPurchase').")]
    public AudioClip shopPurchaseClip;
    [Tooltip("Endgame panel count tick (name: endgameCountdown). Played N times per count animation; pitch always 1.")]
    public AudioClip endgameCountdownClip;

    [Header("Coin Sound Settings")]
    [Tooltip("Base pitch used for coin collection sounds (before random variance).")]
    public float coinPitchBase = 1f;

    [Tooltip("Random upward pitch shift applied to coin sounds. A value of 0.1 means the pitch is between base and base + 0.1.")]
    public float coinPitchRandomVariance = 0.01f;

    [Header("Diamond Sound Settings")]
    [Tooltip("Base pitch for diamond pickup (before random variance).")]
    public float diamondPitchBase = 1f;
    [Tooltip("Random upward pitch shift for diamond pickup.")]
    public float diamondPitchRandomVariance = 0.01f;

    [Header("Powerup Sound Settings")]
    [Tooltip("Base pitch for powerup pickup (before random variance).")]
    public float powerupPitchBase = 1f;
    [Tooltip("Random upward pitch shift for powerup pickup.")]
    public float powerupPitchRandomVariance = 0.01f;

    [Header("Additional Sound Effects (optional)")]
    [Tooltip("Optional extra sound effects. You can leave this empty if you only use wall/coin/bouncyPlatform.")]
    public List<SoundEffect> soundEffects = new List<SoundEffect>();
    
    [System.Serializable]
    public class SoundEffect
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
    }

    private Queue<AudioSource> audioSourcePool;
    private Dictionary<string, SoundEffect> soundEffectDictionary;
    private AudioSource[] audioSources;
    private int currentAudioSourceIndex = 0;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
            BuildSoundEffectDictionary();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        SetMasterVolume(AudioVolumeSettings.GetSfxVolume());
    }

    /// <summary>
    /// Initialize the pool of AudioSource components
    /// </summary>
    private void InitializeAudioSources()
    {
        audioSources = new AudioSource[audioSourcePoolSize];
        audioSourcePool = new Queue<AudioSource>();

        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            GameObject audioSourceObject = new GameObject($"AudioSource_{i}");
            audioSourceObject.transform.SetParent(transform);
            AudioSource source = audioSourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            audioSources[i] = source;
            audioSourcePool.Enqueue(source);
        }
    }

    /// <summary>One shot of the endgame count tick at pitch 1 (uses pooled AudioSource).</summary>
    public void PlayEndgameCountdownOneShot(float volumeOverride = -1f)
    {
        if (!HasEndgameCountdownClip())
            return;
        PlaySound("endgameCountdown", volumeOverride, 1f);
    }

    public bool HasEndgameCountdownClip()
    {
        return endgameCountdownClip != null;
    }

    /// <summary>
    /// Build a dictionary for quick sound effect lookups
    /// </summary>
    private void BuildSoundEffectDictionary()
    {
        soundEffectDictionary = new Dictionary<string, SoundEffect>();

        // Always register the three core sounds with fixed names if clips are assigned.
        AddCoreSound("wall", wallClip);
        AddCoreSound("coin", coinClip);
        AddCoreSound("bouncyPlatform", bouncyPlatformClip);
        AddCoreSound("anvil", anvilClip);
        AddCoreSound("chest", chestClip);
        AddCoreSound("diamond", diamondClip);
        AddCoreSound("powerup", powerupClip);
        AddCoreSound("homeTowerSwipe", homeTowerSwipeClip);
        AddCoreSound("shopPurchase", shopPurchaseClip);
        AddCoreSound("endgameCountdown", endgameCountdownClip);

        // Register any additional sounds from the list.
        foreach (var soundEffect in soundEffects)
        {
            if (soundEffect != null && !string.IsNullOrEmpty(soundEffect.name))
            {
                if (soundEffectDictionary.ContainsKey(soundEffect.name))
                {
                }
                else
                {
                    soundEffectDictionary[soundEffect.name] = soundEffect;
                }
            }
        }
    }

    void AddCoreSound(string name, AudioClip clip)
    {
        if (clip == null) return;

        if (!soundEffectDictionary.ContainsKey(name))
        {
            soundEffectDictionary[name] = new SoundEffect
            {
                name = name,
                clip = clip,
                volume = 1f
            };
        }
    }

    /// <summary>
    /// Play a sound effect by name
    /// </summary>
    /// <param name="soundName">The name of the sound effect as defined in the inspector</param>
    /// <param name="volumeOverride">Optional volume override (0-1). If not provided, uses the sound effect's default volume.</param>
    /// <param name="pitchOverride">Optional pitch override (0.5-3.0). If not provided, uses default pitch of 1.0.</param>
    public void PlaySound(string soundName, float volumeOverride = -1f, float pitchOverride = -1f)
    {
        if (string.IsNullOrEmpty(soundName))
        {
            return;
        }

        if (!soundEffectDictionary.ContainsKey(soundName))
        {
            return;
        }

        SoundEffect soundEffect = soundEffectDictionary[soundName];
        
        if (soundEffect.clip == null)
        {
            return;
        }

        // Get an available AudioSource from the pool
        AudioSource source = GetAvailableAudioSource();
        if (source == null)
        {
            return;
        }

        // Configure and play the sound
        source.clip = soundEffect.clip;
        float finalVolume = volumeOverride >= 0f ? volumeOverride : soundEffect.volume;
        source.volume = finalVolume * masterVolume;
        source.pitch = pitchOverride >= 0f ? Mathf.Clamp(pitchOverride, 0.5f, 3f) : 1f;
        source.Play();
    }

    /// <summary>
    /// Plays the coin pickup sound using the pitch settings exposed in the Inspector.
    /// </summary>
    /// <param name="volumeOverride">Optional volume override (0-1). If not provided, uses the sound effect's default volume.</param>
    public void PlayCoinSound(float volumeOverride = -1f)
    {
        // Pitch only shifts upward (base to base + variance).
        float upwardShift = Mathf.Max(0f, coinPitchRandomVariance);
        float pitch = coinPitchBase + Random.Range(0f, upwardShift);
        pitch = Mathf.Clamp(pitch, 0.5f, 3f);
        PlaySound("coin", volumeOverride, pitch);
    }

    /// <summary>
    /// Plays the diamond/gem pickup sound (same pattern as coins: slight upward pitch variance).
    /// </summary>
    public void PlayDiamondSound(float volumeOverride = -1f)
    {
        float upwardShift = Mathf.Max(0f, diamondPitchRandomVariance);
        float pitch = diamondPitchBase + Random.Range(0f, upwardShift);
        pitch = Mathf.Clamp(pitch, 0.5f, 3f);
        PlaySound("diamond", volumeOverride, pitch);
    }

    /// <summary>
    /// Plays the powerup pickup sound (same pattern as coins: slight upward pitch variance).
    /// </summary>
    public void PlayPowerupSound(float volumeOverride = -1f)
    {
        float upwardShift = Mathf.Max(0f, powerupPitchRandomVariance);
        float pitch = powerupPitchBase + Random.Range(0f, upwardShift);
        pitch = Mathf.Clamp(pitch, 0.5f, 3f);
        PlaySound("powerup", volumeOverride, pitch);
    }

    public void PlayHomeTowerSwipeSound(float volumeOverride = -1f)
    {
        PlaySound("homeTowerSwipe", volumeOverride, 1f);
    }

    public void PlayShopPurchaseSound(float volumeOverride = -1f)
    {
        PlaySound("shopPurchase", volumeOverride, 1f);
    }

    /// <summary>
    /// Play a sound effect using an AudioClip directly (for dynamically loaded clips)
    /// </summary>
    /// <param name="clip">The AudioClip to play</param>
    /// <param name="volume">Volume level (0-1)</param>
    /// <param name="pitch">Pitch level (0.5-3.0), default is 1.0</param>
    public void PlaySound(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource source = GetAvailableAudioSource();
        if (source == null)
        {
            return;
        }

        source.clip = clip;
        source.volume = volume * masterVolume;
        source.pitch = Mathf.Clamp(pitch, 0.5f, 3f);
        source.Play();
    }

    /// <summary>
    /// Get an available AudioSource from the pool
    /// </summary>
    private AudioSource GetAvailableAudioSource()
    {
        // Use round-robin approach for simplicity
        AudioSource source = audioSources[currentAudioSourceIndex];
        
        // If current source is playing, try to find a free one
        if (source.isPlaying)
        {
            for (int i = 0; i < audioSourcePoolSize; i++)
            {
                int index = (currentAudioSourceIndex + i) % audioSourcePoolSize;
                if (!audioSources[index].isPlaying)
                {
                    currentAudioSourceIndex = index;
                    return audioSources[index];
                }
            }
            // All sources are playing, use the current one anyway (will interrupt)
        }

        currentAudioSourceIndex = (currentAudioSourceIndex + 1) % audioSourcePoolSize;
        return source;
    }

    /// <summary>
    /// Stop all currently playing sound effects
    /// </summary>
    public void StopAllSounds()
    {
        foreach (var source in audioSources)
        {
            if (source != null && source.isPlaying)
            {
                source.Stop();
            }
        }
    }

    /// <summary>
    /// Set the master volume for all sound effects
    /// </summary>
    /// <param name="volume">Volume level (0-1)</param>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyMasterVolume();
    }

    /// <summary>
    /// Apply master volume to all AudioSources
    /// </summary>
    private void ApplyMasterVolume()
    {
        // Volume will be applied when sounds are played
    }

    /// <summary>
    /// Check if a sound effect exists
    /// </summary>
    public bool HasSound(string soundName)
    {
        return soundEffectDictionary.ContainsKey(soundName);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
