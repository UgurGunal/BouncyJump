using UnityEngine;

    public class TowerGenerator : MonoBehaviour
{
    public Transform player;
    public LevelManager levelManager;
    public CameraFollow cameraFollow;

    [Header("Platform Generation Settings")]
    public float platformXMin = -2.5f;
    public float platformXMax = 2.5f;
    public float platformYIntervalMin = 2f;
    public float platformYIntervalMax = 4f;
    public float platformScaleXMin = 0.9f;
    public float platformScaleXMax = 1.1f;

    [Header("Collectable Generation Settings")]
    public float collectableXMin = -2.5f;
    public float collectableXMax = 2.5f;
    public float collectableYOffsetMin = 1f;
    public float collectableYOffsetMax = 2f;

    private float lastSpawnedPlatformY = 0f;
    private int currentLevel = -1;
    private Transform generatedObjectsParent;

    void Start()
    {
        if (player == null || levelManager == null || cameraFollow == null)
        {
            
            return;
        }

        // Create a parent object for generated items
        generatedObjectsParent = new GameObject("GeneratedObjects").transform;

        // Initial spawn
        SpawnInitialPlatforms();
    }

    void Update()
    {
        if (player == null) return;

        // Spawn new platforms as player moves up
        while (lastSpawnedPlatformY < player.position.y + 10f)
        {
            // Check for level change
            int newLevel = levelManager.GetCurrentLevel(lastSpawnedPlatformY);
            if (newLevel != currentLevel)
            {
                currentLevel = newLevel;
                UpdateLevelSettings(currentLevel);
            }
            SpawnPlatform();
        }
    }

    void SpawnInitialPlatforms()
    {
        // Spawn a few platforms at the beginning
        for (int i = 0; i < 10; i++)
        {
            SpawnPlatform();
        }
    }

    void UpdateLevelSettings(int level)
    {
        LevelManager.LevelData levelData = levelManager.GetLevelData(level);
        cameraFollow.constantSpeed = levelData.cameraSpeed;
    }

    void SpawnPlatform()
    {
        LevelManager.LevelData levelData = levelManager.GetLevelData(currentLevel);

        // Choose platform type based on spawn rates
        float totalPlatformSpawnRate = levelData.longPlatformSpawnRate + levelData.shortPlatformSpawnRate + levelData.specialPlatformSpawnRate;
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

        // Determine platform position
        float platformX = Random.Range(platformXMin, platformXMax);
        float platformY = lastSpawnedPlatformY + Random.Range(platformYIntervalMin, platformYIntervalMax);
        Vector3 platformPosition = new Vector3(platformX, platformY, 0);

        // Instantiate platform
        GameObject newPlatform = Instantiate(platformPrefab, platformPosition, Quaternion.identity);
        newPlatform.transform.SetParent(generatedObjectsParent);

        // Set platform scale
        float platformScaleX = Random.Range(platformScaleXMin, platformScaleXMax);
        newPlatform.transform.localScale = new Vector3(platformScaleX, newPlatform.transform.localScale.y, newPlatform.transform.localScale.z);

        lastSpawnedPlatformY = platformY;

        // Spawn collectable
        SpawnCollectable(platformY);
    }

    void SpawnCollectable(float platformY)
    {
        LevelManager.LevelData levelData = levelManager.GetLevelData(currentLevel);

        // Choose collectable type based on spawn rates
        float totalCollectableSpawnRate = levelData.coin1SpawnRate + levelData.coin2SpawnRate + levelData.powerupSpawnRate + levelData.diamondSpawnRate + levelData.emptySpawnRate;
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
            // Determine collectable position
            float collectableX = Random.Range(collectableXMin, collectableXMax);
            float collectableY = platformY + Random.Range(collectableYOffsetMin, collectableYOffsetMax);
            Vector3 collectablePosition = new Vector3(collectableX, collectableY, 0);

            // Instantiate collectable
            GameObject newCollectable = Instantiate(collectablePrefab, collectablePosition, Quaternion.identity);
            newCollectable.transform.SetParent(generatedObjectsParent);
        }
    }
}