using UnityEngine;

public class SimpleTowerGenerator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player will be auto-found if not assigned")]
    public Transform player;
    [Tooltip("LevelManager will be auto-found from active tower scene")]
    public LevelManager levelManager;
    
    [Header("Spawn Settings")]
    public float spawnHeightOffset = 8f;
    public bool enableCollectableSpawning = true;
    
    private Transform generatedObjectsParent;
    private float lastSpawnedPlatformY = -3f;
    private int currentLevel = -1;

    void Start()
    {
        // Use coroutine to wait for tower scene to load and find references
        StartCoroutine(InitializeAfterTowerSceneLoaded());
    }
    
    System.Collections.IEnumerator InitializeAfterTowerSceneLoaded()
    {
        // Wait a moment for tower scene to fully load
        yield return new WaitForSeconds(0.1f);
        
        // Auto-find references
        FindRequiredReferences();
        
        // Check if we have everything we need
        if (player == null || levelManager == null)
        {
            Debug.LogError("SimpleTowerGenerator: Missing required references after auto-find");
            Debug.LogError($"Player: {(player != null ? "Found" : "Missing")}, LevelManager: {(levelManager != null ? "Found" : "Missing")}");
            yield break;
        }

        // Create parent for generated objects
        generatedObjectsParent = new GameObject("GeneratedObjects").transform;
        
        Debug.Log("[SimpleTowerGenerator] Initialization complete, starting content generation");
        
        // Spawn initial content
        SpawnInitialContent();
    }
    
    void FindRequiredReferences()
    {
        // Auto-find player if not assigned
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                Debug.Log("[SimpleTowerGenerator] Auto-found Player");
            }
            else
            {
                Debug.LogWarning("[SimpleTowerGenerator] Player not found! Make sure Player has 'Player' tag");
            }
        }
        
        // Auto-find LevelManager from the active tower scene
        if (levelManager == null)
        {
            levelManager = LevelManager.Instance;
            if (levelManager != null)
            {
                Debug.Log("[SimpleTowerGenerator] Auto-found LevelManager from tower scene");
            }
            else
            {
                Debug.LogWarning("[SimpleTowerGenerator] LevelManager not found! Make sure tower scene has LevelManager with singleton pattern");
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        // Check for level change
        int newLevel = levelManager.GetCurrentLevel(lastSpawnedPlatformY);
        if (newLevel != currentLevel)
        {
            currentLevel = newLevel;
            OnLevelChanged(newLevel);
        }

        // Spawn new content as player moves up
        while (lastSpawnedPlatformY < player.position.y + spawnHeightOffset)
        {
            SpawnLevelContent();
        }
    }

    void SpawnInitialContent()
    {
        // Spawn initial platforms
        for (int i = 0; i < 10; i++)
        {
            SpawnLevelContent();
        }
    }

    void SpawnLevelContent()
    {
        // Calculate the Y position for the next platform first
        float nextPlatformY = lastSpawnedPlatformY + GetRandomYInterval(1.8f, 1.9f);
        
        // Get level data based on the platform's Y position (not player position)
        int platformLevel = levelManager.GetCurrentLevel(nextPlatformY);
        LevelManager.LevelData levelData = levelManager.GetLevelData(platformLevel);
        if (levelData == null) return;

        // Spawn platform with collectable using level data for this Y position
        SpawnPlatformWithCollectable(levelData, nextPlatformY);
        
        // Update last spawned Y position
        lastSpawnedPlatformY = nextPlatformY;
    }

    void SpawnPlatformWithCollectable(LevelManager.LevelData levelData, float platformY)
    {
        // Choose platform type based on spawn rates
        float totalPlatformSpawnRate = levelData.longPlatformSpawnRate + 
                                      levelData.shortPlatformSpawnRate + 
                                      levelData.specialPlatformSpawnRate;
        float randomPlatformValue = Random.Range(0, totalPlatformSpawnRate);

        GameObject platformPrefab;
        if (randomPlatformValue < levelData.longPlatformSpawnRate)
        {
            platformPrefab = levelData.longPlatformPrefab;
        }
        else if (randomPlatformValue < levelData.longPlatformSpawnRate + levelData.shortPlatformSpawnRate)
        {
            platformPrefab = levelData.shortPlatformPrefab;
        }
        else
        {
            platformPrefab = levelData.specialPlatformPrefab;
        }

        // Determine platform position using default settings
        float platformX = GetRandomXPosition(-1.82f, 1.82f);
        // platformY is now passed as parameter, no need to calculate again
        float platformScaleX = Random.Range(0.85f, 0.95f);

        // Spawn platform
        SpawnPlatform(platformX, platformY, platformPrefab, platformScaleX);

        // Spawn collectable
        SpawnCollectableForPlatform(levelData, platformY);
    }

    void SpawnCollectableForPlatform(LevelManager.LevelData levelData, float platformY)
    {
        // Choose collectable type based on spawn rates from level manager
        float totalCollectableSpawnRate = levelData.coin1SpawnRate + 
                                        levelData.coin2SpawnRate + 
                                        levelData.powerupSpawnRate + 
                                        levelData.diamondSpawnRate + 
                                        levelData.emptySpawnRate;
        float randomCollectableValue = Random.Range(0, totalCollectableSpawnRate);

        GameObject collectablePrefab = null;
        if (randomCollectableValue < levelData.coin1SpawnRate)
        {
            collectablePrefab = levelManager.coin1Prefab;
        }
        else if (randomCollectableValue < levelData.coin1SpawnRate + levelData.coin2SpawnRate)
        {
            collectablePrefab = levelManager.coin2Prefab;
        }
        else if (randomCollectableValue < levelData.coin1SpawnRate + levelData.coin2SpawnRate + levelData.powerupSpawnRate)
        {
            collectablePrefab = levelManager.powerupPrefab;
        }
        else if (randomCollectableValue < levelData.coin1SpawnRate + levelData.coin2SpawnRate + levelData.powerupSpawnRate + levelData.diamondSpawnRate)
        {
            collectablePrefab = levelManager.diamondPrefab;
        }

        if (collectablePrefab != null)
        {
            // Determine collectable position using default settings
            float collectableX = GetRandomXPosition(-1.5f, 1.5f);
            float collectableY = platformY + Random.Range(0.65f, 0.9f);

            // Spawn collectable
            SpawnCollectable(collectableX, collectableY, collectablePrefab);
        }
    }

    void SpawnPlatform(float xPosition, float yPosition, GameObject platformPrefab, float scaleX = 1f)
    {
        if (platformPrefab == null) return;

        Vector3 platformPosition = new Vector3(xPosition, yPosition, 0);
        GameObject newPlatform = Instantiate(platformPrefab, platformPosition, Quaternion.identity);
        newPlatform.transform.SetParent(generatedObjectsParent);

        // Set platform scale
        newPlatform.transform.localScale = new Vector3(scaleX, newPlatform.transform.localScale.y, newPlatform.transform.localScale.z);
    }

    void SpawnCollectable(float xPosition, float yPosition, GameObject collectablePrefab)
    {
        if (!enableCollectableSpawning || collectablePrefab == null) return;

        Vector3 collectablePosition = new Vector3(xPosition, yPosition, 0);
        GameObject newCollectable = Instantiate(collectablePrefab, collectablePosition, Quaternion.identity);
        newCollectable.transform.SetParent(generatedObjectsParent);
    }

    void OnLevelChanged(int newLevel)
    {
        Debug.Log($"SimpleTowerGenerator: Level changed to {newLevel}");
    }

    float GetRandomXPosition(float minX, float maxX)
    {
        return Random.Range(minX, maxX);
    }

    float GetRandomYInterval(float minInterval, float maxInterval)
    {
        return Random.Range(minInterval, maxInterval);
    }
}
