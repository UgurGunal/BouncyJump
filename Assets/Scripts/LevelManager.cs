using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; } // New: Singleton Instance

    public int levelCount = 4;
    public float levelHeight = 20f;

    [Header("Player and Camera References")]
    [Tooltip("These will be auto-found from GamePersistentScene if not assigned")]
    public Transform player;
    public CameraFollow cameraFollow;



    public GameObject coin1Prefab;
    public GameObject coin2Prefab;
    public GameObject coin3Prefab;
    public GameObject powerupPrefab;
    public GameObject diamondPrefab;

    [Header("Height Labels (Optional â€” per tower)")]
    [Tooltip("When off, platforms spawn without height labels. Configure separately on each tower's LevelManager.")]
    public bool enableHeightLabels = false;
    [Tooltip("Optional cap: max display height for labels (100, 200, â€¦). 0 = use every entry in Height Label Prefabs.")]
    public float towerHeight = 0f;
    [Tooltip("Display-height interval for which platform gets a label (100, 200, 300, â€¦). Label position uses the platform's real X/Y, not these exact heights.")]
    public float heightLabelInterval = 100f;
    [Tooltip("Used only to test when a platform has passed a milestone (display height = world Y Ã— this). Matches HUD (Ã—5).")]
    public float heightDisplayMultiplier = 5f;
    [Tooltip("Prefab for 100 label (index 0), 200 (index 1), etc.")]
    public GameObject[] heightLabelPrefabs;
    [Tooltip("Extra gap above the platform bottom edge (world units). 0 = bottom of label flush with lower border.")]
    public float heightLabelYOffset = 0f;
    [Tooltip("Fine-tune label Y after border alignment (negative = slightly lower).")]
    public float heightLabelYPadding = -0.2f;
    [Tooltip("Labels on platforms past this X are shifted inward (e.g. platform 1.8 â†’ label 1.4).")]
    public float heightLabelMaxX = 1.4f;
    [Tooltip("Labels on platforms past this X are shifted inward (e.g. platform -1.5 â†’ label -1.4).")]
    public float heightLabelMinX = -1.4f;

    [System.Serializable]
    public class LevelData
    {
        public float cameraSpeed = 5f;
        public GameObject longPlatformPrefab;
        public GameObject shortPlatformPrefab;
        public GameObject specialPlatformPrefab;
        public GameObject specialPlatform2Prefab;
        public float longPlatformSpawnRate = 1f;
        public float shortPlatformSpawnRate = 1f;
        public float specialPlatformSpawnRate = 1f;
        public float specialPlatform2SpawnRate = 1f;
        public float coin1SpawnRate = 1f;
        public float coin2SpawnRate = 1f;
        public float coin3SpawnRate = 1f;
        public float powerupSpawnRate = 1f;
        public float diamondSpawnRate = 1f;
        public float emptySpawnRate = 5f;

        [Header("Level Change UI")]
        [Tooltip("Color of the brief LEVEL text shown when entering this level.")]
        public Color levelChangeTextColor = Color.white;
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
        // Use coroutine to ensure GamePersistentScene is loaded before finding references
        StartCoroutine(InitializeAfterPersistentScene());
    }
    
    System.Collections.IEnumerator InitializeAfterPersistentScene()
    {
        // Wait for persistent scene to be loaded
        yield return new WaitUntil(() => PersistentLoader.AreGameManagersLoaded());
        
        // Small additional delay to ensure all objects are initialized
        yield return new WaitForEndOfFrame();
        
        // Auto-find references from GamePersistentScene if not assigned
        FindPersistentReferences();
        
        if (PointsManager.Instance != null)
        {
            PointsManager.Instance.StartSession();
        }
        
    }
    
    void FindPersistentReferences()
    {
        // Auto-find player if not assigned
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (cameraFollow == null)
            cameraFollow = FindObjectOfType<CameraFollow>();
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

    public LevelData GetCurrentLevelData()
    {
        return GetLevelData(currentLevel);
    }

    public bool AreHeightLabelsEnabled()
    {
        return enableHeightLabels
            && heightLabelPrefabs != null
            && heightLabelPrefabs.Length > 0
            && heightLabelInterval > 0f
            && heightDisplayMultiplier > 0f;
    }

    public int GetMaxHeightLabelCount()
    {
        if (!AreHeightLabelsEnabled()) return 0;

        int prefabCount = heightLabelPrefabs.Length;
        if (heightLabelInterval <= 0f)
            return prefabCount;

        int fromTowerHeight = Mathf.Max(0, Mathf.FloorToInt(towerHeight / heightLabelInterval));
        if (fromTowerHeight <= 0)
            return prefabCount;

        return Mathf.Min(prefabCount, fromTowerHeight);
    }

    public bool TryGetHeightLabelPrefab(int labelIndex, out GameObject prefab)
    {
        prefab = null;
        if (!AreHeightLabelsEnabled() || labelIndex < 0 || labelIndex >= heightLabelPrefabs.Length)
            return false;

        prefab = heightLabelPrefabs[labelIndex];
        return prefab != null;
    }

    public float GetClampedHeightLabelX(float platformX)
    {
        return Mathf.Clamp(platformX, heightLabelMinX, heightLabelMaxX);
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
            LevelChangeUI.Instance.ShowLevelChange(level, levelData.levelChangeTextColor);
        }
        
    }
}
