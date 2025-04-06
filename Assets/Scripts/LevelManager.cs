using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [System.Serializable]
    public class PlatformConfig
    {
        public GameObject platformShortPrefab; // Short version of first platform
        public GameObject platformLongPrefab;  // Long version of first platform
        public float platformMinXScale;
        public float platformMaxXScale;
        public float platformYScale;
        public float platformSpawnRate;
    }

    [System.Serializable]
    public class PlatformSimpleConfig
    {
        public GameObject platformPrefab;
        public float platformMinXScale;
        public float platformMaxXScale;
        public float platformYScale;
        public float platformSpawnRate;
    }

    [System.Serializable]
    public class LevelConfig
    {
        public float upwardCameraSpeed; // Camera speed
        public float powerUpSpawnRate;  // Power-up spawn rate
        public PlatformConfig firstPlatformConfig;     // Includes short/long prefabs
        public PlatformSimpleConfig secondPlatformConfig; // Second platform remains simple
    }

    public LevelConfig[] levels;
    public int currentLevel = 0;

    public Transform player; // Player reference
    public int nextLevelThreshold = 800;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ApplyLevelSettings();
    }

    private void Update()
    {
        if (player == null) return;

        // Move to next level if threshold reached
        if (player.position.y >= (currentLevel + 1) * nextLevelThreshold)
        {
            NextLevel();
        }
    }

    public void NextLevel()
    {
        if (currentLevel < levels.Length - 1)
        {
            currentLevel++;
            ApplyLevelSettings();
        }
        else
        {
            Debug.Log("Game Completed! Restarting...");
            RestartGame();
        }
    }

    private void ApplyLevelSettings()
    {
        Debug.Log("Applying Level " + (currentLevel + 1));

        // Apply camera speed for new level
        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.SetCameraSpeed(levels[currentLevel].upwardCameraSpeed);
        }
    }

    public void RestartGame()
    {
        currentLevel = 0;
        ApplyLevelSettings();
    }
}
