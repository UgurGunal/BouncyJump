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
    private Coroutine pauseFadeCoroutine;
    private bool resumeFadeInProgress;

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
        string activeSceneName = SceneManager.GetActiveScene().name;

        // Ignore additive loads (e.g. persistent managers) — they are not the gameplay scene.
        if (mode == LoadSceneMode.Additive && scene.name != activeSceneName)
            return;

        if (mode == LoadSceneMode.Single)
        {
            if (activeSceneName != currentSceneName)
            {
                currentSceneName = activeSceneName;
                PlayMusicForCurrentScene();
            }
            else
            {
                RestartMusic();
            }

            return;
        }

        if (activeSceneName != currentSceneName)
        {
            currentSceneName = activeSceneName;
            PlayMusicForCurrentScene();
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
        if (musicSource.clip == null)
            return;

        StopPauseFadeCoroutine();
        resumeFadeInProgress = false;

        if (!musicSource.isPlaying)
            return;

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
    /// Continues the current track from its paused position (no restart). Safe to call once.
    /// </summary>
    public void ResumeMusic()
    {
        if (musicSource == null || musicSource.clip == null)
            return;

        if (resumeFadeInProgress)
            return;

        float targetVolume = GetTargetVolumeForCurrentClip();
        if (musicSource.isPlaying && musicSource.volume >= targetVolume * 0.95f)
            return;

        StopPauseFadeCoroutine();

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        resumeFadeInProgress = true;
        pauseFadeCoroutine = StartCoroutine(FadeInResumeCoroutine(1f));
    }

    float GetTargetVolumeForCurrentClip()
    {
        float baseVolume = defaultMusicVolume;
        if (!string.IsNullOrEmpty(currentSceneName) && sceneMusicDictionary != null &&
            sceneMusicDictionary.ContainsKey(currentSceneName))
        {
            baseVolume = sceneMusicDictionary[currentSceneName].volume;
        }

        return baseVolume * masterVolume;
    }

    void StopPauseFadeCoroutine()
    {
        if (pauseFadeCoroutine == null)
            return;

        StopCoroutine(pauseFadeCoroutine);
        pauseFadeCoroutine = null;
    }

    /// <summary>
    /// Coroutine to fade in music volume after resume (does not restart the clip).
    /// </summary>
    private IEnumerator FadeInResumeCoroutine(float fadeDuration)
    {
        float targetVolume = savedVolume > 0f ? savedVolume : GetTargetVolumeForCurrentClip();

        if (!musicSource.isPlaying)
            musicSource.UnPause();

        float elapsedTime = 0f;
        float startVolume = musicSource.volume;

        while (elapsedTime < fadeDuration)
        {
            if (!musicSource.isPlaying)
                break;

            elapsedTime += Time.unscaledDeltaTime;
            float t = elapsedTime / fadeDuration;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        if (musicSource.isPlaying)
            musicSource.volume = targetVolume;

        resumeFadeInProgress = false;
        pauseFadeCoroutine = null;
    }

    /// <summary>
    /// Stop gameplay music immediately (e.g. after game over). Does not start a new track.
    /// </summary>
    public void StopMusic()
    {
        StopPauseFadeCoroutine();
        resumeFadeInProgress = false;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.time = 0f;
            musicSource.volume = 0f;
        }
    }

    /// <summary>
    /// Restart the currently playing music from the beginning
    /// </summary>
    public void RestartMusic()
    {
        StopPauseFadeCoroutine();
        resumeFadeInProgress = false;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        currentSceneName = SceneManager.GetActiveScene().name;
        PlayMusicForCurrentScene();
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
