using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; } // New: Singleton Instance

    public int levelCount = 4;
    public float levelHeight = 20f;

    [Header("Player and Camera References")]
    public Transform player;
    public CameraFollow cameraFollow;

    public GameObject coin1Prefab;
    public GameObject coin2Prefab;
    public GameObject powerupPrefab;
    public GameObject diamondPrefab;

    [System.Serializable]
    public class LevelData
    {
        public float cameraSpeed = 5f;
        public GameObject longPlatformPrefab;
        public GameObject shortPlatformPrefab;
        public GameObject specialPlatformPrefab;
        public float longPlatformSpawnRate = 1f;
        public float shortPlatformSpawnRate = 1f;
        public float specialPlatformSpawnRate = 1f;
        public float coin1SpawnRate = 1f;
        public float coin2SpawnRate = 1f;
        public float powerupSpawnRate = 1f;
        public float diamondSpawnRate = 1f;
        public float emptySpawnRate = 5f;
    }

    public LevelData[] levels;

    private int currentLevel = -1;
    private float lastCheckTime = 0f;
    private float checkInterval = 1f; // Check every 1 second

    // New: Awake method for Singleton
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: DontDestroyOnLoad(gameObject); if you want it to persist across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (PointsManager.Instance != null)
        {
            PointsManager.Instance.StartSession();
        }
    }

    void Update()
    {
        if (player == null) return;

        // Check for level change based on player's Y position every second
        if (Time.time - lastCheckTime >= checkInterval)
        {
            int newLevel = GetCurrentLevel(player.position.y);
            if (newLevel != currentLevel)
            {
                currentLevel = newLevel;
                UpdateLevelSettings(currentLevel);
            }
            lastCheckTime = Time.time;
        }
    }

    public int GetCurrentLevel(float playerY)
    {
        return Mathf.Max(1, Mathf.FloorToInt(playerY / levelHeight) + 1);
    }

    public LevelData GetLevelData(int level)
    {
        // Convert level 1-based to 0-based array index
        int arrayIndex = Mathf.Clamp(level - 1, 0, levels.Length - 1);
        return levels[arrayIndex];
    }

    void UpdateLevelSettings(int level)
    {
        LevelData levelData = GetLevelData(level);
        
        // Update camera speed using the new method
        if (cameraFollow != null)
        {
            cameraFollow.UpdateCameraSpeed(levelData.cameraSpeed);
        }
        
        // Show level change UI
        if (LevelChangeUI.Instance != null)
        {
            LevelChangeUI.Instance.ShowLevelChange(level);
        }
        
        Debug.Log($"Level changed to {level}, Camera speed: {levelData.cameraSpeed}");
    }
}