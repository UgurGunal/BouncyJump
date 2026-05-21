using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class ChestPlatform : MonoBehaviour
{
    static readonly List<ChestPlatform> ActiveChestPlatforms = new List<ChestPlatform>();

    [Header("Animation Settings")]
    public Animator chestAnimator;
    [Tooltip("Animator state played when the chest opens (e.g. ChestOpen).")]
    public string openAnimatorStateName = "ChestOpen";
    [Tooltip("Animator state when closed. Leave empty to use default state on layer 0.")]
    public string closedAnimatorStateName = "";
    public SpriteRenderer chestSpriteRenderer;
    public Sprite closedSprite;
    public Sprite openedSprite;
    
    [Header("Collectable Prefabs")]
    public GameObject coin1Prefab;
    public GameObject coin2Prefab;
    public GameObject coin3Prefab;
    public GameObject diamond1Prefab;
    public GameObject diamond2Prefab;
    
    [Header("Spawn Choice System")]
    public bool useSpawnChoiceSystem = true; // Enable/disable the spawn choice system
    
    [Header("Y Position Ranges and Probabilities")]
    [Range(0, 200)]
    public float range1Max = 200f; // 0-200
    public float range1Spawn1Prob = 37f; // 3x coin1
    public float range1Spawn2Prob = 30f; // 1x coin1, 1x coin2, 1x coin3
    public float range1Spawn3Prob = 20f; // 3x coin2
    public float range1Spawn4Prob = 10.5f; // 3x coin3
    public float range1Spawn5Prob = 1.5f;  // 3x diamond1
    public float range1Spawn6Prob = 0.75f;  // 3x diamond2
    
    [Range(200, 400)]
    public float range2Max = 400f; // 200-400
    public float range2Spawn1Prob = 30f; // 3x coin1
    public float range2Spawn2Prob = 30f; // 1x coin1, 1x coin2, 1x coin3
    public float range2Spawn3Prob = 24f; // 3x coin2
    public float range2Spawn4Prob = 11f; // 3x coin3
    public float range2Spawn5Prob = 3.5f;  // 3x diamond1
    public float range2Spawn6Prob = 1.5f;  // 3x diamond2
    
    [Range(400, 600)]
    public float range3Max = 600f; // 400-600
    public float range3Spawn1Prob = 20f; // 3x coin1
    public float range3Spawn2Prob = 30f; // 1x coin1, 1x coin2, 1x coin3
    public float range3Spawn3Prob = 25f; // 3x coin2
    public float range3Spawn4Prob = 18f; // 3x coin3
    public float range3Spawn5Prob = 5f;  // 3x diamond1
    public float range3Spawn6Prob = 2f;  // 3x diamond2
    
    [Range(600, 800)]
    public float range4Max = 800f; // 600-800
    public float range4Spawn1Prob = 10f; // 3x coin1
    public float range4Spawn2Prob = 20f; // 1x coin1, 1x coin2, 1x coin3
    public float range4Spawn3Prob = 32f; // 3x coin2
    public float range4Spawn4Prob = 28f; // 3x coin3
    public float range4Spawn5Prob = 7.5f; // 3x diamond1
    public float range4Spawn6Prob = 2.5f;  // 3x diamond2
    
    [Range(800, 1000)]
    public float range5Max = 1000f; // 800-1000
    public float range5Spawn1Prob = 0f; // 3x coin1
    public float range5Spawn2Prob = 25f; // 1x coin1, 1x coin2, 1x coin3
    public float range5Spawn3Prob = 25f; // 3x coin2
    public float range5Spawn4Prob = 37f; // 3x coin3
    public float range5Spawn5Prob = 9f; // 3x diamond1
    public float range5Spawn6Prob = 4f; // 3x diamond2
    
    public float range6Spawn1Prob = 0f;  // 3x coin1 (1000+)
    public float range6Spawn2Prob = 10f; // 1x coin1, 1x coin2, 1x coin3
    public float range6Spawn3Prob = 20f; // 3x coin2
    public float range6Spawn4Prob = 50f; // 3x coin3
    public float range6Spawn5Prob = 14f; // 3x diamond1
    public float range6Spawn6Prob = 6f;  // 3x diamond2

    

    
    [Header("Spawn Position Settings")]
    public float spawnHeightOffset = 0.4f; // Height above platform to spawn collectables (increased to avoid platform collisions)

    [Header("Chest Collectable Launch")]
    [Tooltip("How long the coin arc animation takes (seconds).")]
    public float collectableLaunchDuration = 0.9f;
    [Tooltip("Seconds after spawn before the coin collider turns on (can collect). Usually ≤ launch duration.")]
    public float collectableColliderEnableDelay = 0.9f;
    [Tooltip("Peak height of the coin arc above the straight path (world units).")]
    public float collectableArcHeight = 2.6f;
    
    [Header("Debug Options")]
    public bool enableDebugLogging = false; // Enable to see timing information
    
    [Header("Platform Jump Settings")]
    public float jumpForce = 14f; // Base jump force for the platform
    public float comboBonus = 0.005f; // Multiplier for current combo to jump bonus
    
    [Header("Platform Collision Detection")]
    public float velocityThreshold = 5f; // Very lenient velocity check
    public float contactNormalThreshold = 0f; // Very lenient normal check
    
    [Header("Platform Combo System")]
    public bool enableComboSystem = false; // Set to true if you want combo functionality
    
    [Header("Platform Destruction Settings")]
    [Tooltip("Only destruction mode: removes chest when the player is this far above its highest Y.")]
    public bool enableDistanceDestroy = true;
    public float destroyDistance = 8f;

    [Header("Falling Settings")]
    public bool enableFalling = false;
    public float fallMinSpeed = 1f;
    public float fallMaxSpeed = 6f;
    public float fallAccelerationTime = 1.5f;
    [Range(0f, 1f)]
    public float fallTintStrength = 0.15f;
    
    private bool isOpened = false;
    private bool isAnimating = false;
    private bool isFalling;
    private float fallElapsedTime;
    private Transform playerTransform;
    private float destroyReferenceY;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalSpriteColors;

    void OnEnable()
    {
        ActiveChestPlatforms.Add(this);
    }

    void OnDisable()
    {
        ActiveChestPlatforms.Remove(this);
    }

    void Start()
    {
        EnsurePlayerReference();
        destroyReferenceY = transform.position.y;
        CacheSpriteRenderers();
        ResetChestVisualState();
    }

    public void ResetForSpawn(Vector3 worldPosition, Vector3 localScale)
    {
        isFalling = false;
        fallElapsedTime = 0f;
        transform.position = worldPosition;
        transform.localScale = localScale;
        transform.rotation = Quaternion.identity;
        destroyReferenceY = worldPosition.y;
        ClearSpawnedCollectables();
        RestoreSpriteColors();
        ResetChestVisualState();
    }

    public void PrepareForPool()
    {
        isFalling = false;
        fallElapsedTime = 0f;
        ClearSpawnedCollectables();
        RestoreSpriteColors();
        ResetChestVisualState();
    }

    void EnsurePlayerReference()
    {
        if (playerTransform != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            playerTransform = playerObject.transform;
    }

    void CacheSpriteRenderers()
    {
        if (chestSpriteRenderer != null)
            spriteRenderers = new[] { chestSpriteRenderer };
        else
            spriteRenderers = GetComponents<SpriteRenderer>();

        originalSpriteColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            originalSpriteColors[i] = spriteRenderers[i].color;
    }

    void RestoreSpriteColors()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            CacheSpriteRenderers();

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
                continue;

            spriteRenderers[i].color = originalSpriteColors[i];
        }
    }

    public void StartFalling()
    {
        if (!enableFalling || isFalling)
            return;

        isFalling = true;
        fallElapsedTime = 0f;
        ApplyFallTint();
    }

    void ApplyFallTint()
    {
        if (fallTintStrength <= 0f)
            return;

        if (spriteRenderers == null || spriteRenderers.Length == 0)
            CacheSpriteRenderers();

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
                continue;

            spriteRenderers[i].color = Color.Lerp(originalSpriteColors[i], Color.black, fallTintStrength);
        }
    }

    public static void TriggerFallForChestsBelow(float collidedPlatformY)
    {
        for (int i = ActiveChestPlatforms.Count - 1; i >= 0; i--)
        {
            ChestPlatform chest = ActiveChestPlatforms[i];
            if (chest == null)
            {
                ActiveChestPlatforms.RemoveAt(i);
                continue;
            }

            if (chest.transform.position.y < collidedPlatformY)
                chest.StartFalling();
        }
    }

    void Despawn()
    {
        PooledInstance pooled = GetComponent<PooledInstance>();
        if (pooled != null)
            pooled.Release();
        else
            Destroy(gameObject);
    }

    void ClearSpawnedCollectables()
    {
        // Chest coins/diamonds live in the world like tower collectables — do not return them to the pool.
        StopAllCoroutines();
    }

    void ResetChestVisualState()
    {
        isOpened = false;
        isAnimating = false;

        if (chestSpriteRenderer != null && closedSprite != null)
            chestSpriteRenderer.sprite = closedSprite;

        if (chestAnimator != null)
        {
            chestAnimator.Rebind();
            chestAnimator.Update(0f);

            if (string.IsNullOrEmpty(closedAnimatorStateName))
                chestAnimator.Play(0, 0, 0f);
            else
                chestAnimator.Play(closedAnimatorStateName, 0, 0f);
        }
    }
    
    private void FixedUpdate()
    {
        EnsurePlayerReference();
        if (playerTransform == null)
            return;

        destroyReferenceY = Mathf.Max(destroyReferenceY, transform.position.y);

        if (enableDistanceDestroy && playerTransform.position.y > destroyReferenceY + destroyDistance)
        {
            Despawn();
            return;
        }

        if (!isFalling)
            return;

        fallElapsedTime += Time.fixedDeltaTime;
        float t = fallAccelerationTime > 0f
            ? Mathf.Clamp01(fallElapsedTime / fallAccelerationTime)
            : 1f;
        float fallSpeed = Mathf.Lerp(fallMinSpeed, fallMaxSpeed, t);
        transform.position += Vector3.down * fallSpeed * Time.fixedDeltaTime;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerBallController player = collision.gameObject.GetComponent<PlayerBallController>();
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            
            // Always handle jumping (platform functionality)
            HandleJump(player, rb, collision);
        }
    }
    
    private void HandleJump(PlayerBallController player, Rigidbody2D rb, Collision2D collision)
    {
        // Simplified and more reliable detection
        bool isOnTop = false;

        // Method 1: Contact normal detection (very lenient)
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < contactNormalThreshold)
            {
                isOnTop = true;
                break;
            }
        }

        // Method 2: Simple position check (if normal detection fails)
        if (!isOnTop)
        {
            isOnTop = player.transform.position.y > transform.position.y;
        }

        // Method 3: Very lenient velocity check
        bool isNotMovingUp = rb.velocity.y <= velocityThreshold;

        // Jump if player is on top AND not moving upward
        if (isOnTop && isNotMovingUp)
        {
            // Start chest opening BEFORE jumping (if not already opened and not animating)
            if (!isOpened && !isAnimating)
            {
                StartChestOpening();
            }
            
            // Calculate relative velocity for combo increment
            float relativeVelocity = Mathf.Abs(collision.relativeVelocity.y);

            // Increment combo if combo system is enabled
            if (enableComboSystem)
            {
                IncrementPlatformCombo(relativeVelocity);
            }

            // Calculate jump bonus using current combo value (if combo system is enabled)
            float jumpBonus = 0f;
            if (enableComboSystem)
            {
                jumpBonus = GetComboBonus();
            }

            // Apply jump with bonus: platform jump force + combo jump bonus
            float totalJumpForce = jumpForce + jumpBonus;
            player.Jump(totalJumpForce);
            
            // Play platform collision sound effect
            if (SoundEffectsManager.Instance != null)
            {
                float pitchVariance = Random.Range(-0.1f, 0.1f);
                float pitch = 1f + pitchVariance;
                SoundEffectsManager.Instance.PlaySound("platform", -1f, pitch);
            }

            float collidedPlatformY = transform.position.y;
            StartFalling();
            Platform.TriggerFallForPlatformsBelow(collidedPlatformY);
            TriggerFallForChestsBelow(collidedPlatformY);
        }
    }
    
    private void StartChestOpening()
    {
        if (isOpened || isAnimating) return;
        
        isAnimating = true;

        // Play chest sound only when opening is actually triggered.
        if (SoundEffectsManager.Instance != null)
        {
            SoundEffectsManager.Instance.PlaySound("chest");
        }
        
        // Start the opening animation directly by name
        if (chestAnimator != null && !string.IsNullOrEmpty(openAnimatorStateName))
            chestAnimator.Play(openAnimatorStateName, 0, 0f);
        
        // Start coroutine to spawn collectables with delay
        StartCoroutine(SpawnCollectablesWithDelay());
        
        // Start coroutine to handle the complete opening sequence
        StartCoroutine(ChestOpeningSequence());
    }
    
    private IEnumerator ChestOpeningSequence()
    {
        // Wait for animation to complete (you can adjust this time based on your animation length)
        float animationDuration = 1f; // Default animation duration
        if (chestAnimator != null)
        {
            // Try to get animation clip length
            AnimatorStateInfo stateInfo = chestAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.length > 0)
            {
                animationDuration = stateInfo.length;
            }
        }
        
        yield return new WaitForSeconds(animationDuration);
        
        // Mark as opened
        isOpened = true;
        isAnimating = false;
    }
    
    private IEnumerator SpawnCollectablesWithDelay()
    {
        // Wait 0.1 seconds before spawning collectables
        yield return new WaitForSeconds(0.16f);
        
        // Spawn collectables after the delay
        if (useSpawnChoiceSystem)
        {
            // Use the new spawn choice system based on Y position
            ChooseAndSpawnCollectables();
        }
    }
    
    private void SpawnCollectables()
    {
        if (useSpawnChoiceSystem)
        {
            // Use the new spawn choice system based on Y position
            ChooseAndSpawnCollectables();
        }

    }
    
    private void ChooseAndSpawnCollectables()
    {
        float chestYPosition = transform.position.y;
        
        // Determine spawn choice based on Y position
        if (chestYPosition <= range1Max)
        {
            // Range 1: 0-200
            ChooseSpawnByProbability(range1Spawn1Prob, range1Spawn2Prob, range1Spawn3Prob, 
                                   range1Spawn4Prob, range1Spawn5Prob, range1Spawn6Prob);
        }
        else if (chestYPosition <= range2Max)
        {
            // Range 2: 200-400
            ChooseSpawnByProbability(range2Spawn1Prob, range2Spawn2Prob, range2Spawn3Prob, 
                                   range2Spawn4Prob, range2Spawn5Prob, range2Spawn6Prob);
        }
        else if (chestYPosition <= range3Max)
        {
            // Range 3: 400-600
            ChooseSpawnByProbability(range3Spawn1Prob, range3Spawn2Prob, range3Spawn3Prob, 
                                   range3Spawn4Prob, range3Spawn5Prob, range3Spawn6Prob);
        }
        else if (chestYPosition <= range4Max)
        {
            // Range 4: 600-800
            ChooseSpawnByProbability(range4Spawn1Prob, range4Spawn2Prob, range4Spawn3Prob, 
                                   range4Spawn4Prob, range4Spawn5Prob, range4Spawn6Prob);
        }
        else if (chestYPosition <= range5Max)
        {
            // Range 5: 800-1000
            ChooseSpawnByProbability(range5Spawn1Prob, range5Spawn2Prob, range5Spawn3Prob, 
                                   range5Spawn4Prob, range5Spawn5Prob, range5Spawn6Prob);
        }
        else
        {
            // Range 6: 1000+
            ChooseSpawnByProbability(range6Spawn1Prob, range6Spawn2Prob, range6Spawn3Prob, 
                                   range6Spawn4Prob, range6Spawn5Prob, range6Spawn6Prob);
        }
    }
    
    private void ChooseSpawnByProbability(float spawn1Prob, float spawn2Prob, float spawn3Prob, 
                                        float spawn4Prob, float spawn5Prob, float spawn6Prob)
    {
        float random = Random.Range(0f, 100f);
        float cumulativeProb = 0f;
        
        // Spawn 1: 3x coin1
        cumulativeProb += spawn1Prob;
        if (random <= cumulativeProb)
        {
            SpawnCoin1(3);
            return;
        }
        
        // Spawn 2: 1x coin1, 1x coin2, 1x coin3
        cumulativeProb += spawn2Prob;
        if (random <= cumulativeProb)
        {
            SpawnCoin1(1);
            SpawnCoin2(1);
            SpawnCoin3(1);
            return;
        }
        
        // Spawn 3: 3x coin2
        cumulativeProb += spawn3Prob;
        if (random <= cumulativeProb)
        {
            SpawnCoin2(3);
            return;
        }
        
        // Spawn 4: 3x coin3
        cumulativeProb += spawn4Prob;
        if (random <= cumulativeProb)
        {
            SpawnCoin3(3);
            return;
        }
        
        // Spawn 5: 3x diamond1
        cumulativeProb += spawn5Prob;
        if (random <= cumulativeProb)
        {
            SpawnDiamond1(3);
            return;
        }
        
        // Spawn 6: 3x diamond2
        cumulativeProb += spawn6Prob;
        if (random <= cumulativeProb)
        {
            SpawnDiamond2(3);
            return;
        }
        
        // Fallback: If no spawn was chosen, use spawn 1 (3x coin1)
        SpawnCoin1(3);
    }
    
    private Vector3 CalculateRandomTargetPosition()
    {
        // Use absolute world coordinates for X position (not relative to chest)
        float randomX = Random.Range(-1.5f, 1.5f); // Absolute world X between -1.5 and +1.5
        float randomY = Random.Range(4.6f, 5.6f);
        
        // Add additional height multiplier based on platform height
        float platformHeight = transform.position.y;
        float heightMultiplier = 1f / 800f;
        float additionalHeight = platformHeight * heightMultiplier;
        
        // Calculate target position with absolute X, but Y relative to chest plus additional height
        Vector3 targetPosition = new Vector3(randomX, transform.position.y + randomY + additionalHeight, 0);
        
        // Ensure target position is not on top of a platform by checking for collisions
        // Check multiple points around the coin's final position to ensure complete clearance
        float coinRadius = 0.5f; // Approximate coin radius for collision detection
        bool hasPlatformCollision = false;
        
        // Check center point
        Vector2 centerCheck = new Vector2(targetPosition.x, targetPosition.y);
        if (Physics2D.OverlapPoint(centerCheck) != null)
        {
            hasPlatformCollision = true;
        }
        
        // Check left edge
        Vector2 leftCheck = new Vector2(targetPosition.x - coinRadius, targetPosition.y);
        if (Physics2D.OverlapPoint(leftCheck) != null)
        {
            hasPlatformCollision = true;
        }
        
        // Check right edge
        Vector2 rightCheck = new Vector2(targetPosition.x + coinRadius, targetPosition.y);
        if (Physics2D.OverlapPoint(rightCheck) != null)
        {
            hasPlatformCollision = true;
        }
        
        // Check bottom center (most important for landing)
        Vector2 bottomCheck = new Vector2(targetPosition.x, targetPosition.y - coinRadius);
        if (Physics2D.OverlapPoint(bottomCheck) != null)
        {
            hasPlatformCollision = true;
        }
        
        // If there's any platform collision, move the coin higher
        if (hasPlatformCollision)
        {
            targetPosition.y += 3f; // Add 3 units above any detected platform
        }
        
        return targetPosition;
    }
    

    
    public void ResetChest()
    {
        ClearSpawnedCollectables();
        RestoreSpriteColors();
        ResetChestVisualState();
    }
    
    // Platform combo system methods
    private void IncrementPlatformCombo(float relativeVelocity)
    {
        // Try to increment combo safely without direct ComboManager reference
        try
        {
            // Use reflection to safely access ComboManager
            System.Type comboManagerType = System.Type.GetType("ComboManager");
            if (comboManagerType != null)
            {
                var instanceProperty = comboManagerType.GetProperty("Instance");
                if (instanceProperty != null)
                {
                    var instance = instanceProperty.GetValue(null);
                    if (instance != null)
                    {
                        var platformComboMethod = comboManagerType.GetMethod("PlatformComboIncrement");
                        if (platformComboMethod != null)
                        {
                            platformComboMethod.Invoke(instance, new object[] { relativeVelocity });
                        }
                    }
                }
            }
        }
        catch (System.Exception)
        {
            // ComboManager not available
        }
    }

    private float GetComboBonus()
    {
        // Try to get combo bonus safely without direct ComboManager reference
        try
        {
            // Use reflection to safely access ComboManager
            System.Type comboManagerType = System.Type.GetType("ComboManager");
            if (comboManagerType != null)
            {
                var instanceProperty = comboManagerType.GetProperty("Instance");
                if (instanceProperty != null)
                {
                    var instance = instanceProperty.GetValue(null);
                    if (instance != null)
                    {
                        var getComboMethod = comboManagerType.GetMethod("getCombo");
                        if (getComboMethod != null)
                        {
                            float currentCombo = (float)getComboMethod.Invoke(instance, null);
                            return currentCombo * comboBonus;
                        }
                    }
                }
            }
        }
        catch (System.Exception)
        {
            // ComboManager not available, return 0 bonus
        }

        return 0f;
    }

    // Public methods to control combo system manually
    public void SetComboBonus(float bonus)
    {
        comboBonus = bonus;
    }

    public void EnableComboSystem(bool enable)
    {
        enableComboSystem = enable;
    }

    // Public methods to adjust collision detection
    public void SetVelocityThreshold(float threshold)
    {
        velocityThreshold = threshold;
    }

    public void SetContactNormalThreshold(float threshold)
    {
        contactNormalThreshold = threshold;
    }
    
    // Public methods to spawn specific collectables
    public void SpawnCoin1(int amount = 1)
    {
        if (coin1Prefab == null) return;
        
        for (int i = 0; i < amount; i++)
        {
            SpawnCollectableAtRandomPosition(coin1Prefab);
        }
    }
    
    public void SpawnCoin2(int amount = 1)
    {
        if (coin2Prefab == null) return;
        
        for (int i = 0; i < amount; i++)
        {
            SpawnCollectableAtRandomPosition(coin2Prefab);
        }
    }
    
    public void SpawnCoin3(int amount = 1)
    {
        if (coin3Prefab == null) return;
        
        for (int i = 0; i < amount; i++)
        {
            SpawnCollectableAtRandomPosition(coin3Prefab);
        }
    }
    
    public void SpawnDiamond1(int amount = 1)
    {
        if (diamond1Prefab == null) return;
        
        for (int i = 0; i < amount; i++)
        {
            SpawnCollectableAtRandomPosition(diamond1Prefab);
        }
    }
    
    public void SpawnDiamond2(int amount = 1)
    {
        if (diamond2Prefab == null) return;
        
        for (int i = 0; i < amount; i++)
        {
            SpawnCollectableAtRandomPosition(diamond2Prefab);
        }
    }
    
    // Helper method to spawn a collectable at a random position
    private void SpawnCollectableAtRandomPosition(GameObject prefab)
    {
        if (prefab == null) return;
        
        // Calculate spawn position just above the platform with small variance
        Vector3 spawnPosition = transform.position + Vector3.up * spawnHeightOffset;
        
        // Add small random variance to Y position only (X variance causes boundary issues)
        float yVariance = Random.Range(-0.2f, 0.2f);
        spawnPosition += new Vector3(0f, yVariance, 0);
        
        GameObject collectable = null;
        if (SimpleTowerGenerator.Instance != null)
            collectable = SimpleTowerGenerator.Instance.SpawnPooledCollectable(prefab, spawnPosition, deferDistanceDestroy: true);

        if (collectable == null)
        {
            collectable = Instantiate(prefab, spawnPosition, Quaternion.identity);
            CollectableSpawnHelper.SetDistanceDestroySuppressed(collectable, true);
        }

        if (collectable == null)
            return;

        Vector3 targetPosition = CalculateRandomTargetPosition();
        ChestCollectableLaunch launch = collectable.GetComponent<ChestCollectableLaunch>();
        if (launch == null)
            launch = collectable.AddComponent<ChestCollectableLaunch>();

        launch.BeginLaunch(
            spawnPosition,
            targetPosition,
            collectableLaunchDuration,
            collectableColliderEnableDelay,
            collectableArcHeight);
    }
}
