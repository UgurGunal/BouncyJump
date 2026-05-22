using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Manages background music for the game.
/// This is a singleton that persists across all scenes (should be placed in the persistent scene).
/// Music changes automatically based on the current scene.
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music Settings")]
    [Tooltip("Fade duration when transitioning between music tracks (in seconds)")]
    public float fadeDuration = 0.5f;
    
    [Range(0f, 1f)]
    public float masterVolume = 0.5f;
    
    [Tooltip("Should music loop?")]
    public bool loopMusic = true;

    [Header("Scene Music Mapping")]
    [Tooltip("Map scene names to music clips. Scene name should match exactly (case-sensitive)")]
    public List<SceneMusic> sceneMusicList = new List<SceneMusic>();
    
    [System.Serializable]
    public class SceneMusic
    {
        [Tooltip("Scene name (must match exactly with the scene name in Build Settings)")]
        public string sceneName;
        
        [Tooltip("Music clip to play for this scene")]
        public AudioClip musicClip;
        
        [Range(0f, 1f)]
        [Tooltip("Volume for this specific music (0-1)")]
        public float volume = 1f;
    }

    [Header("Default Music")]
    [Tooltip("Music to play if no scene-specific music is found")]
    public AudioClip defaultMusic;
    
    [Range(0f, 1f)]
    [Tooltip("Volume for default music (0-1)")]
    public float defaultMusicVolume = 1f;

    private AudioSource musicSource;
    private AudioSource fadeSource; // Used for crossfading
    private Dictionary<string, SceneMusic> sceneMusicDictionary;
    private string currentSceneName;
    private Coroutine fadeCoroutine;
    private float savedVolume; // Store volume before fade out for resume
    private Coroutine pauseFadeCoroutine; // Coroutine for fade out and pause

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
            BuildSceneMusicDictionary();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Subscribe to scene loaded events
        SceneManager.sceneLoaded += OnSceneLoaded;

        SetMasterVolume(AudioVolumeSettings.GetMusicVolume());

        // Play music for the current scene
        PlayMusicForCurrentScene();
    }

    /// <summary>
    /// Initialize AudioSource components for music
    /// </summary>
    private void InitializeAudioSources()
    {
        // Main music source
        GameObject musicObject = new GameObject("MusicSource");
        musicObject.transform.SetParent(transform);
        musicSource = musicObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = loopMusic;
        musicSource.volume = masterVolume;

        // Fade source for crossfading
        GameObject fadeObject = new GameObject("FadeSource");
        fadeObject.transform.SetParent(transform);
        fadeSource = fadeObject.AddComponent<AudioSource>();
        fadeSource.playOnAwake = false;
        fadeSource.loop = loopMusic;
        fadeSource.volume = 0f;
    }

    /// <summary>
    /// Build a dictionary for quick scene music lookups
    /// </summary>
    private void BuildSceneMusicDictionary()
    {
        sceneMusicDictionary = new Dictionary<string, SceneMusic>();
        foreach (var sceneMusic in sceneMusicList)
        {
            if (sceneMusic != null && !string.IsNullOrEmpty(sceneMusic.sceneName))
            {
                if (sceneMusicDictionary.ContainsKey(sceneMusic.sceneName))
                {
                }
                else
                {
                    sceneMusicDictionary[sceneMusic.sceneName] = sceneMusic;
                }
            }
        }
    }

    /// <summary>
    /// Called when a scene is loaded
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only handle additive scenes if needed, or handle main scene changes
        // For now, we'll check the active scene
        if (mode == LoadSceneMode.Single || mode == LoadSceneMode.Additive)
        {
            string newSceneName = SceneManager.GetActiveScene().name;
            if (newSceneName != currentSceneName)
            {
                // Different scene - play music for new scene
                currentSceneName = newSceneName;
                PlayMusicForCurrentScene();
            }
            else
            {
                // Same scene reloaded (restart) - restart music from beginning
                RestartMusic();
            }
        }
    }

    /// <summary>
    /// Play music for the current active scene
    /// </summary>
    private void PlayMusicForCurrentScene()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        currentSceneName = activeSceneName;

        // Try to find scene-specific music
        if (sceneMusicDictionary.ContainsKey(activeSceneName))
        {
            SceneMusic sceneMusic = sceneMusicDictionary[activeSceneName];
            if (sceneMusic.musicClip != null)
            {
                PlayMusic(sceneMusic.musicClip, sceneMusic.volume);
                return;
            }
        }

        // If no scene-specific music found, try default music
        if (defaultMusic != null)
        {
            PlayMusic(defaultMusic, defaultMusicVolume);
        }
    }

    /// <summary>
    /// Play a specific music clip with volume
    /// </summary>
    /// <param name="clip">The music clip to play</param>
    /// <param name="volume">Volume level (0-1)</param>
    public void PlayMusic(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            return;
        }

        // If same clip is already playing, don't restart it
        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        // Stop any ongoing fade
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Start fade transition
        fadeCoroutine = StartCoroutine(FadeToNewMusic(clip, volume));
    }

    /// <summary>
    /// Fade from current music to new music
    /// </summary>
    private IEnumerator FadeToNewMusic(AudioClip newClip, float newVolume)
    {
        float elapsedTime = 0f;
        float startVolume = musicSource.volume;
        float targetVolume = newVolume * masterVolume;

        // If music is playing, fade out current music
        if (musicSource.isPlaying && musicSource.clip != null)
        {
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / fadeDuration;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }
        }

        // Switch to new music
        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.Play();

        // Fade in new music
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        musicSource.volume = targetVolume;
        fadeCoroutine = null;
    }

    /// <summary>
    /// Stop the currently playing music
    /// </summary>
    public void StopMusic()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        if (musicSource.isPlaying)
        {
            StartCoroutine(FadeOutMusic());
        }
    }

    /// <summary>
    /// Fade out current music
    /// </summary>
    private IEnumerator FadeOutMusic()
    {
        float elapsedTime = 0f;
        float startVolume = musicSource.volume;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = 0f;
    }

    /// <summary>
    /// Pause the currently playing music (instant pause, no fade)
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    /// <summary>
    /// Fade out music over 1 second and then pause it (smooth pause for player death)
    /// </summary>
    public void FadeOutAndPauseMusic()
    {
        if (!musicSource.isPlaying || musicSource.clip == null)
        {
            return;
        }

        // Stop any existing fade coroutine
        if (pauseFadeCoroutine != null)
        {
            StopCoroutine(pauseFadeCoroutine);
        }

        pauseFadeCoroutine = StartCoroutine(FadeOutAndPauseCoroutine(1f));
    }

    /// <summary>
    /// Coroutine to fade out music and then pause it
    /// </summary>
    private IEnumerator FadeOutAndPauseCoroutine(float fadeDuration)
    {
        float elapsedTime = 0f;
        savedVolume = musicSource.volume; // Save current volume for resume

        // Fade out the volume
        while (elapsedTime < fadeDuration && musicSource.isPlaying)
        {
            elapsedTime += Time.unscaledDeltaTime; // Use unscaled time in case game is paused
            float t = elapsedTime / fadeDuration;
            musicSource.volume = Mathf.Lerp(savedVolume, 0f, t);
            yield return null;
        }

        // Ensure volume is 0 and pause
        musicSource.volume = 0f;
        if (musicSource.isPlaying)
        {
            musicSource.Pause();
        }

        pauseFadeCoroutine = null;
    }

    /// <summary>
    /// Resume paused music with a fade in
    /// </summary>
    public void ResumeMusic()
    {
        if (musicSource.clip == null)
        {
            return;
        }

        // Stop any existing fade coroutine
        if (pauseFadeCoroutine != null)
        {
            StopCoroutine(pauseFadeCoroutine);
            pauseFadeCoroutine = null;
        }

        // Resume playback
        if (!musicSource.isPlaying)
        {
            musicSource.UnPause();
        }

        // Fade in the volume over 1 second
        StartCoroutine(FadeInResumeCoroutine(1f));
    }

    /// <summary>
    /// Coroutine to fade in music volume after resume
    /// </summary>
    private IEnumerator FadeInResumeCoroutine(float fadeDuration)
    {
        // Get the target volume (use saved volume if available, otherwise calculate from scene settings)
        float targetVolume = savedVolume;
        if (targetVolume <= 0f)
        {
            // Calculate target volume from scene settings
            float baseVolume = defaultMusicVolume;
            if (sceneMusicDictionary.ContainsKey(currentSceneName))
            {
                baseVolume = sceneMusicDictionary[currentSceneName].volume;
            }
            targetVolume = baseVolume * masterVolume;
        }

        float elapsedTime = 0f;
        float startVolume = musicSource.volume;

        // Fade in the volume
        while (elapsedTime < fadeDuration && musicSource.isPlaying)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = elapsedTime / fadeDuration;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        // Ensure volume is at target
        musicSource.volume = targetVolume;
    }

    /// <summary>
    /// Restart the currently playing music from the beginning
    /// </summary>
    public void RestartMusic()
    {
        // Stop any ongoing fade
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        // If we have a clip and it matches the current scene, restart it
        if (musicSource.clip != null)
        {
            // Stop and restart from beginning
            musicSource.Stop();
            
            // Get the appropriate volume for current music
            float volume = defaultMusicVolume;
            if (sceneMusicDictionary.ContainsKey(currentSceneName))
            {
                volume = sceneMusicDictionary[currentSceneName].volume;
            }
            
            musicSource.volume = volume * masterVolume;
            musicSource.Play();
        }
        else
        {
            // If no clip is set, play music for current scene
            PlayMusicForCurrentScene();
        }
    }

    /// <summary>
    /// Set the master volume for music
    /// </summary>
    /// <param name="volume">Volume level (0-1)</param>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        
        // Update current music volume proportionally
        if (musicSource.isPlaying && musicSource.clip != null)
        {
            // Find the base volume for current music
            float baseVolume = defaultMusicVolume;
            if (sceneMusicDictionary.ContainsKey(currentSceneName))
            {
                baseVolume = sceneMusicDictionary[currentSceneName].volume;
            }
            
            musicSource.volume = baseVolume * masterVolume;
        }
    }

    /// <summary>
    /// Manually change music for a specific scene (useful for special cases)
    /// </summary>
    /// <param name="sceneName">Scene name</param>
    public void ChangeMusicForScene(string sceneName)
    {
        if (sceneMusicDictionary.ContainsKey(sceneName))
        {
            SceneMusic sceneMusic = sceneMusicDictionary[sceneName];
            if (sceneMusic.musicClip != null)
            {
                PlayMusic(sceneMusic.musicClip, sceneMusic.volume);
            }
        }
        else if (defaultMusic != null)
        {
            PlayMusic(defaultMusic, defaultMusicVolume);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from scene events
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
