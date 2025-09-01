using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CollectableSpawnData
{
    public GameObject collectablePrefab;
    public int amount = 1;
}

public class ChestPlatform : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator chestAnimator;
    public string openAnimationTrigger = "Open";
    public SpriteRenderer chestSpriteRenderer;
    public Sprite closedSprite;
    public Sprite openedSprite;
    
    [Header("Collectable Spawn Settings")]
    public List<CollectableSpawnData> collectablesToSpawn = new List<CollectableSpawnData>();
    
    [Header("Spawn Position Settings")]
    public float spawnHeightOffset = 1f; // Height above platform to spawn collectables
    public float minSpawnDistance = 2f; // Minimum distance from platform center
    public float maxSpawnDistance = 5f; // Maximum distance from platform center
    public float spawnAngleRange = 180f; // Angle range for spawn positions (in degrees)
    
    [Header("Collision Avoidance")]
    public bool enableCollisionAvoidance = true; // Enable/disable collision avoidance
    public float collectableRadius = 0.5f; // Radius to check for collectable collisions
    public float platformRadius = 1f; // Radius to check for platform collisions
    public int maxAttempts = 50; // Maximum attempts to find a valid position
    public LayerMask obstacleLayers = -1; // Layers to check for obstacles
    
    [Header("Position Adjustment")]
    public float platformYAdjustment = 0.6f; // How much to adjust Y when colliding with platform
    public float collectableXYAdjustment = 0.3f; // How much to adjust X and Y when colliding with collectable
    public float maxYAdjustment = 10f; // Maximum Y adjustment to prevent going too high
    
    [Header("Lerp Movement Settings")]
    public float lerpDuration = 2f; // Duration of the lerp movement
    public AnimationCurve lerpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Collision Detection")]
    public float velocityThreshold = 5f;
    public float contactNormalThreshold = 0f;
    
    private bool isOpened = false;
    private bool isAnimating = false;
    private Transform playerTransform;
    private Vector3 originalPosition;
    
    private void Start()
    {
        // Find the player by tag
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        
        // Store original position
        originalPosition = transform.position;
        
        // Set initial sprite
        if (chestSpriteRenderer != null && closedSprite != null)
        {
            chestSpriteRenderer.sprite = closedSprite;
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerBallController player = collision.gameObject.GetComponent<PlayerBallController>();
        if (player != null && !isOpened && !isAnimating)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
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
            // Apply jump to player
            player.Jump(14f); // Default jump force
            
            // Start chest opening sequence
            StartChestOpening();
        }
    }
    
    private void StartChestOpening()
    {
        if (isOpened || isAnimating) return;
        
        isAnimating = true;
        
        // Start the opening animation
        if (chestAnimator != null)
        {
            chestAnimator.SetTrigger(openAnimationTrigger);
        }
        
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
        
        // Spawn collectables
        SpawnCollectables();
        
        // Mark as opened
        isOpened = true;
        isAnimating = false;
    }
    
    private void SpawnCollectables()
    {
        if (collectablesToSpawn == null || collectablesToSpawn.Count == 0) return;
        
        foreach (CollectableSpawnData spawnData in collectablesToSpawn)
        {
            if (spawnData.collectablePrefab == null || spawnData.amount <= 0) continue;
            
            for (int i = 0; i < spawnData.amount; i++)
            {
                // Calculate spawn position above the platform
                Vector3 spawnPosition = transform.position + Vector3.up * spawnHeightOffset;
                
                // Instantiate the collectable
                GameObject collectable = Instantiate(spawnData.collectablePrefab, spawnPosition, Quaternion.identity);
                
                // Calculate random target position
                Vector3 targetPosition = CalculateRandomTargetPosition();
                
                // Start lerp movement
                StartCoroutine(LerpCollectableToPosition(collectable, spawnPosition, targetPosition));
            }
        }
    }
    
    private Vector3 CalculateRandomTargetPosition()
    {
        if (!enableCollisionAvoidance)
        {
            // Original simple random position calculation
            float randomAngle = Random.Range(-spawnAngleRange / 2f, spawnAngleRange / 2f);
            float randomDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 direction = Quaternion.Euler(0, 0, randomAngle) * Vector3.right;
            Vector3 targetPosition = transform.position + direction * randomDistance;
            targetPosition.y = Mathf.Max(targetPosition.y, transform.position.y + spawnHeightOffset);
            return targetPosition;
        }
        
        // Try to find a valid position with collision avoidance and adjustment
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Calculate random position
            float randomAngle = Random.Range(-spawnAngleRange / 2f, spawnAngleRange / 2f);
            float randomDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 direction = Quaternion.Euler(0, 0, randomAngle) * Vector3.right;
            Vector3 candidatePosition = transform.position + direction * randomDistance;
            candidatePosition.y = Mathf.Max(candidatePosition.y, transform.position.y + spawnHeightOffset);
            
            // Try to adjust position if there are collisions
            Vector3 adjustedPosition = AdjustPositionForCollisions(candidatePosition);
            
            // Check if the adjusted position is valid
            if (IsPositionValid(adjustedPosition))
            {
                return adjustedPosition;
            }
        }
        
        // If no valid position found after max attempts, return a fallback position
        Debug.LogWarning($"ChestPlatform: Could not find valid position after {maxAttempts} attempts. Using fallback position.");
        float fallbackAngle = Random.Range(-spawnAngleRange / 2f, spawnAngleRange / 2f);
        float fallbackDistance = maxSpawnDistance; // Use max distance as fallback
        Vector3 fallbackDirection = Quaternion.Euler(0, 0, fallbackAngle) * Vector3.right;
        Vector3 fallbackPosition = transform.position + fallbackDirection * fallbackDistance;
        fallbackPosition.y = Mathf.Max(fallbackPosition.y, transform.position.y + spawnHeightOffset);
        return fallbackPosition;
    }
    
    private Vector3 AdjustPositionForCollisions(Vector3 originalPosition)
    {
        Vector3 adjustedPosition = originalPosition;
        
        // Check for collectable collisions and adjust X and Y
        Collider2D[] collectableColliders = Physics2D.OverlapCircleAll(originalPosition, collectableRadius);
        foreach (Collider2D collider in collectableColliders)
        {
            // Check if it's a collectable (has collectable components)
            if (collider.GetComponent<CoinCollectable>() != null || 
                collider.GetComponent<GemCollectable>() != null ||
                collider.GetComponent<PowerupCollectable>() != null)
            {
                // Adjust both X and Y when colliding with collectable
                float randomXOffset = Random.Range(-collectableXYAdjustment, collectableXYAdjustment);
                float randomYOffset = Random.Range(-collectableXYAdjustment, collectableXYAdjustment);
                
                adjustedPosition.x += randomXOffset;
                adjustedPosition.y += randomYOffset;
                
                // Ensure Y doesn't go too high
                adjustedPosition.y = Mathf.Min(adjustedPosition.y, transform.position.y + maxYAdjustment);
                
                break; // Only adjust once per position
            }
        }
        
        // Check for platform collisions and adjust Y
        Collider2D[] platformColliders = Physics2D.OverlapCircleAll(adjustedPosition, platformRadius, obstacleLayers);
        foreach (Collider2D collider in platformColliders)
        {
            // Check if it's a platform (has platform components)
            if (collider.GetComponent<Platform>() != null || 
                collider.GetComponent<ChestPlatform>() != null ||
                collider.CompareTag("Platform"))
            {
                // Adjust Y when colliding with platform (move up)
                adjustedPosition.y += platformYAdjustment;
                
                // Ensure Y doesn't go too high
                adjustedPosition.y = Mathf.Min(adjustedPosition.y, transform.position.y + maxYAdjustment);
                
                break; // Only adjust once per position
            }
        }
        
        return adjustedPosition;
    }
    
    private bool IsPositionValid(Vector3 position)
    {
        // Check for collectable collisions
        Collider2D[] collectableColliders = Physics2D.OverlapCircleAll(position, collectableRadius);
        foreach (Collider2D collider in collectableColliders)
        {
            // Check if it's a collectable (has collectable components)
            if (collider.GetComponent<CoinCollectable>() != null || 
                collider.GetComponent<GemCollectable>() != null ||
                collider.GetComponent<PowerupCollectable>() != null)
            {
                return false; // Position is occupied by a collectable
            }
        }
        
        // Check for platform collisions
        Collider2D[] platformColliders = Physics2D.OverlapCircleAll(position, platformRadius, obstacleLayers);
        foreach (Collider2D collider in platformColliders)
        {
            // Check if it's a platform (has platform components)
            if (collider.GetComponent<Platform>() != null || 
                collider.GetComponent<ChestPlatform>() != null ||
                collider.CompareTag("Platform"))
            {
                return false; // Position is occupied by a platform
            }
        }
        
        return true; // Position is valid
    }
    
    private IEnumerator LerpCollectableToPosition(GameObject collectable, Vector3 startPos, Vector3 endPos)
    {
        if (collectable == null) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < lerpDuration && collectable != null)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / lerpDuration;
            float curveValue = lerpCurve.Evaluate(progress);
            
            Vector3 newPosition = Vector3.Lerp(startPos, endPos, curveValue);
            collectable.transform.position = newPosition;
            
            yield return null;
        }
        
        // Ensure final position is set
        if (collectable != null)
        {
            collectable.transform.position = endPos;
        }
    }
    
    // Public methods for runtime configuration
    public void AddCollectableSpawnData(GameObject prefab, int amount)
    {
        CollectableSpawnData newData = new CollectableSpawnData
        {
            collectablePrefab = prefab,
            amount = amount
        };
        collectablesToSpawn.Add(newData);
    }
    
    public void ClearCollectableSpawnData()
    {
        collectablesToSpawn.Clear();
    }
    
    public void SetSpawnBoundaries(float minDistance, float maxDistance, float angleRange)
    {
        minSpawnDistance = minDistance;
        maxSpawnDistance = maxDistance;
        spawnAngleRange = angleRange;
    }
    
    public void SetLerpSettings(float duration, AnimationCurve curve)
    {
        lerpDuration = duration;
        lerpCurve = curve;
    }
    
    public void SetCollisionAvoidanceSettings(bool enable, float collectableRadius, float platformRadius, int maxAttempts, LayerMask obstacleLayers)
    {
        enableCollisionAvoidance = enable;
        this.collectableRadius = collectableRadius;
        this.platformRadius = platformRadius;
        this.maxAttempts = maxAttempts;
        this.obstacleLayers = obstacleLayers;
    }
    
    public void SetPositionAdjustmentSettings(float platformYAdjustment, float collectableXYAdjustment, float maxYAdjustment)
    {
        this.platformYAdjustment = platformYAdjustment;
        this.collectableXYAdjustment = collectableXYAdjustment;
        this.maxYAdjustment = maxYAdjustment;
    }
    
    // Reset chest state (useful for testing or respawning)
    public void ResetChest()
    {
        isOpened = false;
        isAnimating = false;
        
        if (chestSpriteRenderer != null && closedSprite != null)
        {
            chestSpriteRenderer.sprite = closedSprite;
        }
        
        if (chestAnimator != null)
        {
            chestAnimator.ResetTrigger(openAnimationTrigger);
        }
    }
}
