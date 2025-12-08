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
    public float masterVolume = 1f;
    
    [Header("Sound Effects Library")]
    [Tooltip("Define your sound effects here. Key is the sound name, Value is the AudioClip")]
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
            Debug.Log("SoundEffectsManager: Instance created and marked as persistent");
        }
        else
        {
            Debug.Log("SoundEffectsManager: Duplicate instance destroyed");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Apply initial volume settings
        ApplyMasterVolume();
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

    /// <summary>
    /// Build a dictionary for quick sound effect lookups
    /// </summary>
    private void BuildSoundEffectDictionary()
    {
        soundEffectDictionary = new Dictionary<string, SoundEffect>();
        foreach (var soundEffect in soundEffects)
        {
            if (soundEffect != null && !string.IsNullOrEmpty(soundEffect.name))
            {
                if (soundEffectDictionary.ContainsKey(soundEffect.name))
                {
                    Debug.LogWarning($"SoundEffectsManager: Duplicate sound effect name '{soundEffect.name}'. The first one will be used.");
                }
                else
                {
                    soundEffectDictionary[soundEffect.name] = soundEffect;
                }
            }
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
            Debug.LogWarning("SoundEffectsManager: Attempted to play sound with empty name");
            return;
        }

        if (!soundEffectDictionary.ContainsKey(soundName))
        {
            Debug.LogWarning($"SoundEffectsManager: Sound effect '{soundName}' not found in dictionary. Make sure it's added to the soundEffects list in the inspector.");
            return;
        }

        SoundEffect soundEffect = soundEffectDictionary[soundName];
        
        if (soundEffect.clip == null)
        {
            Debug.LogWarning($"SoundEffectsManager: Sound effect '{soundName}' has no AudioClip assigned.");
            return;
        }

        // Get an available AudioSource from the pool
        AudioSource source = GetAvailableAudioSource();
        if (source == null)
        {
            Debug.LogWarning("SoundEffectsManager: No available AudioSource in pool. Consider increasing audioSourcePoolSize.");
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
    /// Play a sound effect using an AudioClip directly (for dynamically loaded clips)
    /// </summary>
    /// <param name="clip">The AudioClip to play</param>
    /// <param name="volume">Volume level (0-1)</param>
    /// <param name="pitch">Pitch level (0.5-3.0), default is 1.0</param>
    public void PlaySound(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("SoundEffectsManager: Attempted to play null AudioClip");
            return;
        }

        AudioSource source = GetAvailableAudioSource();
        if (source == null)
        {
            Debug.LogWarning("SoundEffectsManager: No available AudioSource in pool. Consider increasing audioSourcePoolSize.");
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
        // For currently playing sounds, we'd need to update them here
        // For simplicity, new sounds will use the new volume
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
