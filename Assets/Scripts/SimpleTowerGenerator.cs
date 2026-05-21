using UnityEngine;

public class SimpleTowerGenerator : MonoBehaviour
{
    public static SimpleTowerGenerator Instance { get; private set; }

    [Header("References")]
    [Tooltip("Player will be auto-found if not assigned")]
    public Transform player;
    [Tooltip("LevelManager will be auto-found from active tower scene")]
    public LevelManager levelManager;
    
    [Header("Spawn Settings")]
    public float spawnHeightOffset = 8f;
    public bool enableCollectableSpawning = true;
    
    [Header("Platform Generation Settings")]
    [Tooltip("Minimum Y interval between platforms")]
    public float minPlatformYInterval = 1.8f;
    [Tooltip("Maximum Y interval between platforms")]
    public float maxPlatformYInterval = 1.9f;
    [Tooltip("Minimum X position for platform spawning")]
    public float minPlatformXPosition = -1.82f;
    [Tooltip("Maximum X position for platform spawning")]
    public float maxPlatformXPosition = 1.82f;
    [Tooltip("Minimum X scale for platforms")]
    public float minPlatformScaleX = 0.95f;
    [Tooltip("Maximum X scale for platforms")]
    public float maxPlatformScaleX = 1.05f;
    
    [Header("Special Platform 2 Settings")]
    [Tooltip("Minimum X position for special platform 2 spawning (can be different from regular platforms)")]
    public float minSpecialPlatform2XPosition = -1.5f;
    [Tooltip("Maximum X position for special platform 2 spawning (can be different from regular platforms)")]
    public float maxSpecialPlatform2XPosition = 1.5f;
    
    [Header("Collectable Generation Settings")]
    [Tooltip("Minimum X position for collectable spawning")]
    public float minCollectableXPosition = -1.5f;
    [Tooltip("Maximum X position for collectable spawning")]
    public float maxCollectableXPosition = 1.5f;
    [Tooltip("Minimum Y offset above platform for collectables")]
    public float minCollectableYOffset = 0.65f;
    [Tooltip("Maximum Y offset above platform for collectables")]
    public float maxCollectableYOffset = 0.9f;
    
    [Header("Initial Content Settings")]
    [Tooltip("Number of initial platforms to spawn")]
    public int initialPlatformCount = 10;

    private Transform generatedObjectsParent;
    private Transform poolInactiveRoot;
    private PrefabObjectPool platformPool;
    private PrefabObjectPool labelPool;
    private PrefabObjectPool collectablePool;
    private float lastSpawnedPlatformY = -3f;
    private int currentLevel = -1;
    private float nextHeightLabelThreshold;
    private int heightLabelsSpawned;
    private int maxHeightLabels;
    private GameObject pendingHeightLabelPlatform;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        StartCoroutine(InitializeAfterTowerSceneLoaded());
    }

    public GameObject SpawnPooledCollectable(GameObject prefab, Vector3 worldPosition, bool deferDistanceDestroy = false)
    {
        if (prefab == null || collectablePool == null)
            return null;

        GameObject collectable = collectablePool.Get(prefab, generatedObjectsParent, activate: false);
        if (collectable == null)
            return null;

        CollectablePoolReset.PrepareForSpawn(collectable);
        collectable.transform.position = worldPosition;
        collectable.transform.rotation = Quaternion.identity;

        if (deferDistanceDestroy)
            CollectableSpawnHelper.SetDistanceDestroySuppressed(collectable, true);

        collectable.SetActive(true);
        return collectable;
    }
    
    System.Collections.IEnumerator InitializeAfterTowerSceneLoaded()
    {
        float waitStart = Time.realtimeSinceStartup;
        while (LevelManager.Instance == null && Time.realtimeSinceStartup - waitStart < 5f)
            yield return null;

        yield return new WaitForEndOfFrame();

        FindRequiredReferences();

        if (player == null || levelManager == null)
        {
            Debug.LogError("SimpleTowerGenerator: Missing required references after auto-find");
            Debug.LogError($"Player: {(player != null ? "Found" : "Missing")}, LevelManager: {(levelManager != null ? "Found" : "Missing")}");
            yield break;
        }

        generatedObjectsParent = new GameObject("GeneratedObjects").transform;
        poolInactiveRoot = new GameObject("PooledInactive").transform;
        poolInactiveRoot.SetParent(transform);
        poolInactiveRoot.gameObject.SetActive(false);

        platformPool = new PrefabObjectPool(poolInactiveRoot, ReleasePlatformToPool);
        labelPool = new PrefabObjectPool(poolInactiveRoot, ReleaseLabelToPool);
        collectablePool = new PrefabObjectPool(poolInactiveRoot, ReleaseCollectableToPool);

        RefreshHeightLabelState();
        
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
        if (levelManager == null || levelManager != LevelManager.Instance)
        {
            levelManager = LevelManager.Instance;
            if (levelManager == null) return;
            RefreshHeightLabelState();
        }

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
        for (int i = 0; i < initialPlatformCount; i++)
        {
            SpawnLevelContent();
        }
    }

    void SpawnLevelContent()
    {
        // Calculate the Y position for the next platform first
        float nextPlatformY = lastSpawnedPlatformY + GetRandomYInterval(minPlatformYInterval, maxPlatformYInterval);
        
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
                                      levelData.specialPlatformSpawnRate +
                                      levelData.specialPlatform2SpawnRate;
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
        else if (randomPlatformValue < levelData.longPlatformSpawnRate + levelData.shortPlatformSpawnRate + levelData.specialPlatformSpawnRate)
        {
            platformPrefab = levelData.specialPlatformPrefab;
        }
        else
        {
            platformPrefab = levelData.specialPlatform2Prefab;
        }

        // Determine platform position using default settings
        float platformX;
        // Use special X position range for special platform 2, regular range for other platforms
        if (platformPrefab == levelData.specialPlatform2Prefab)
        {
            platformX = GetRandomXPosition(minSpecialPlatform2XPosition, maxSpecialPlatform2XPosition);
        }
        else
        {
            platformX = GetRandomXPosition(minPlatformXPosition, maxPlatformXPosition);
        }
        // platformY is now passed as parameter, no need to calculate again
        float platformScaleX = Random.Range(minPlatformScaleX, maxPlatformScaleX);

        // Spawn platform
        GameObject newPlatform = SpawnPlatform(platformX, platformY, platformPrefab, platformScaleX);
        TrySpawnHeightLabel(newPlatform);

        // Spawn collectable
        SpawnCollectableForPlatform(levelData, platformY);
    }

    void SpawnCollectableForPlatform(LevelManager.LevelData levelData, float platformY)
    {
        // Choose collectable type based on spawn rates from level manager
        float totalCollectableSpawnRate = levelData.coin1SpawnRate + 
                                        levelData.coin2SpawnRate + 
                                        levelData.coin3SpawnRate +
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
        else if (randomCollectableValue < levelData.coin1SpawnRate + levelData.coin2SpawnRate + levelData.coin3SpawnRate)
        {
            collectablePrefab = levelManager.coin3Prefab;
        }
        else if (randomCollectableValue < levelData.coin1SpawnRate + levelData.coin2SpawnRate + levelData.coin3SpawnRate + levelData.powerupSpawnRate)
        {
            collectablePrefab = levelManager.powerupPrefab;
        }
        else if (randomCollectableValue < levelData.coin1SpawnRate + levelData.coin2SpawnRate + levelData.coin3SpawnRate + levelData.powerupSpawnRate + levelData.diamondSpawnRate)
        {
            collectablePrefab = levelManager.diamondPrefab;
        }

        if (collectablePrefab != null)
        {
            // Determine collectable position using default settings
            float collectableX = GetRandomXPosition(minCollectableXPosition, maxCollectableXPosition);
            float collectableY = platformY + Random.Range(minCollectableYOffset, maxCollectableYOffset);

            // Spawn collectable
            SpawnCollectable(collectableX, collectableY, collectablePrefab);
        }
    }

    GameObject SpawnPlatform(float xPosition, float yPosition, GameObject platformPrefab, float scaleX = 1f)
    {
        if (platformPrefab == null) return null;

        Vector3 platformPosition = new Vector3(xPosition, yPosition, 0);
        GameObject newPlatform = platformPool.Get(platformPrefab, generatedObjectsParent);
        if (newPlatform == null) return null;

        Vector3 scale = new Vector3(scaleX, newPlatform.transform.localScale.y, newPlatform.transform.localScale.z);

        ChestPlatform chestPlatform = newPlatform.GetComponent<ChestPlatform>();
        if (chestPlatform != null)
            chestPlatform.ResetForSpawn(platformPosition, scale);
        else
        {
            Platform platform = newPlatform.GetComponent<Platform>();
            if (platform != null)
                platform.ResetForSpawn(platformPosition, scale);
            else
            {
                newPlatform.transform.position = platformPosition;
                newPlatform.transform.localScale = scale;
            }
        }

        return newPlatform;
    }

    void ReleasePlatformToPool(GameObject prefab, GameObject instance)
    {
        if (instance == null || prefab == null)
            return;

        ReleaseLabelChildren(instance);

        ChestPlatform chestPlatform = instance.GetComponent<ChestPlatform>();
        if (chestPlatform != null)
            chestPlatform.PrepareForPool();
        else
        {
            Platform platform = instance.GetComponent<Platform>();
            if (platform != null)
                platform.PrepareForPool();
        }

        platformPool.Return(prefab, instance);
    }

    void ReleaseLabelChildren(GameObject platformInstance)
    {
        for (int i = platformInstance.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = platformInstance.transform.GetChild(i);
            if (child.GetComponent<HeightLabelMarker>() == null)
                continue;

            PooledInstance pooled = child.GetComponent<PooledInstance>();
            if (pooled != null && pooled.SourcePrefab != null)
                labelPool.Return(pooled.SourcePrefab, child.gameObject);
            else
                Destroy(child.gameObject);
        }
    }

    void ReleaseLabelToPool(GameObject prefab, GameObject instance)
    {
        labelPool.Return(prefab, instance);
    }

    void ReleaseCollectableToPool(GameObject prefab, GameObject instance)
    {
        CollectablePoolReset.PrepareForPool(instance);
        collectablePool.Return(prefab, instance);
    }

    void RefreshHeightLabelState()
    {
        heightLabelsSpawned = 0;
        maxHeightLabels = 0;
        nextHeightLabelThreshold = 100f;
        pendingHeightLabelPlatform = null;

        if (levelManager == null || !levelManager.AreHeightLabelsEnabled()) return;

        nextHeightLabelThreshold = levelManager.heightLabelInterval;
        maxHeightLabels = levelManager.GetMaxHeightLabelCount();
    }

    void TrySpawnHeightLabel(GameObject platform)
    {
        if (platform == null || levelManager == null) return;
        if (!levelManager.AreHeightLabelsEnabled() || heightLabelsSpawned >= maxHeightLabels) return;

        float displayHeight = platform.transform.position.y * levelManager.heightDisplayMultiplier;

        if (displayHeight < nextHeightLabelThreshold)
        {
            pendingHeightLabelPlatform = platform;
            return;
        }

        while (displayHeight >= nextHeightLabelThreshold && heightLabelsSpawned < maxHeightLabels)
        {
            if (pendingHeightLabelPlatform != null)
            {
                SpawnHeightLabelOnPlatform(pendingHeightLabelPlatform, heightLabelsSpawned);
                heightLabelsSpawned++;
            }

            pendingHeightLabelPlatform = null;
            nextHeightLabelThreshold += levelManager.heightLabelInterval;
        }

        if (heightLabelsSpawned < maxHeightLabels && displayHeight < nextHeightLabelThreshold)
            pendingHeightLabelPlatform = platform;
    }

    void SpawnHeightLabelOnPlatform(GameObject platform, int labelIndex)
    {
        if (!levelManager.TryGetHeightLabelPrefab(labelIndex, out GameObject labelPrefab))
            return;

        GameObject label = labelPool.Get(labelPrefab, platform.transform);
        if (label == null)
            return;

        if (label.GetComponent<HeightLabelMarker>() == null)
            label.AddComponent<HeightLabelMarker>();

        ApplyHeightLabelTransform(label, platform);
    }

    void ApplyHeightLabelTransform(GameObject label, GameObject platform)
    {
        Transform platformTransform = platform.transform;
        label.transform.SetParent(platformTransform, false);
        label.transform.localRotation = Quaternion.identity;

        // Counteract platform X scale so the label keeps its intended size/position as a child.
        float platformScaleX = platformTransform.localScale.x;
        if (Mathf.Abs(platformScaleX) < 0.0001f)
            platformScaleX = 1f;
        label.transform.localScale = new Vector3(1f / platformScaleX, 1f, 1f);

        GetPlatformSurfaceLocal(platform, out float localBottomY);

        SpriteRenderer platformRenderer = platform.GetComponent<SpriteRenderer>();
        SpriteRenderer labelRenderer = label.GetComponent<SpriteRenderer>();

        float labelHalfHeight = 0f;
        if (labelRenderer != null && labelRenderer.sprite != null)
            labelHalfHeight = labelRenderer.sprite.bounds.extents.y;

        float localY = localBottomY
            + levelManager.heightLabelYOffset
            + labelHalfHeight
            + levelManager.heightLabelYPadding;

        float clampedWorldX = levelManager.GetClampedHeightLabelX(platformTransform.position.x);
        Vector3 localClampedPoint = platformTransform.InverseTransformPoint(
            new Vector3(clampedWorldX, platformTransform.position.y, platformTransform.position.z));
        float localX = localClampedPoint.x;

        label.transform.localPosition = new Vector3(localX, localY, 0f);

        if (labelRenderer == null)
            return;

        if (platformRenderer != null)
        {
            labelRenderer.sortingLayerID = platformRenderer.sortingLayerID;
            labelRenderer.sortingOrder = platformRenderer.sortingOrder + 2;
        }
        else
            labelRenderer.sortingOrder = 3;
    }

    /// <summary>Bottom edge and horizontal center in platform local space (for child labels).</summary>
    static void GetPlatformSurfaceLocal(GameObject platform, out float localBottomY)
    {
        localBottomY = 0f;

        BoxCollider2D box = platform.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            localBottomY = box.offset.y - box.size.y * 0.5f;
            return;
        }

        SpriteRenderer spriteRenderer = platform.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
            localBottomY = spriteRenderer.sprite.bounds.min.y;
    }

    void SpawnCollectable(float xPosition, float yPosition, GameObject collectablePrefab)
    {
        if (!enableCollectableSpawning || collectablePrefab == null) return;

        Vector3 collectablePosition = new Vector3(xPosition, yPosition, 0);
        GameObject newCollectable = collectablePool.Get(collectablePrefab, generatedObjectsParent);
        if (newCollectable == null) return;

        newCollectable.transform.position = collectablePosition;
        newCollectable.transform.rotation = Quaternion.identity;
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
