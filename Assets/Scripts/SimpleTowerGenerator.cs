using UnityEngine;

public class SimpleTowerGenerator : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public LevelManager levelManager;
    
    [Header("Spawn Settings")]
    public float spawnHeightOffset = 8f;
    public bool enableCollectableSpawning = true;
    
    private Transform generatedObjectsParent;
    private float lastSpawnedPlatformY = -3f;
    private int currentLevel = -1;

    void Start()
    {
        if (player == null || levelManager == null)
        {
            Debug.LogError("SimpleTowerGenerator: Missing required references");
            return;
        }

        // Create parent for generated objects
        generatedObjectsParent = new GameObject("GeneratedObjects").transform;
        
        // Spawn initial content
        SpawnInitialContent();
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
        // Get level data directly from level manager
        LevelManager.LevelData levelData = levelManager.GetCurrentLevelData();
        if (levelData == null) return;

        // Spawn platform with collectable using manual settings
        SpawnPlatformWithCollectable(levelData);
        
        // Update last spawned Y position with default interval
        float platformY = lastSpawnedPlatformY + GetRandomYInterval(1.8f, 1.9f);
        lastSpawnedPlatformY = platformY;
    }

    void SpawnPlatformWithCollectable(LevelManager.LevelData levelData)
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
        float platformY = lastSpawnedPlatformY + GetRandomYInterval(1.8f, 1.9f);
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
