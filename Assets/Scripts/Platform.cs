using UnityEngine;

public class Platform : MonoBehaviour
{
    [Header("Jump Settings")]
    public float jumpForce = 14f;
    public float comboBonus = 0.005f; // Multiplier for current combo to jump bonus

    [Header("Collision Detection")]
    public float velocityThreshold = 5f; // Very lenient velocity check
    public float contactNormalThreshold = 0f; // Very lenient normal check

    [Header("Combo System")]
    public bool enableComboSystem = false; // Set to true if you want combo functionality

    [Header("Destruction Settings")]
    public bool enableTimerDestroy = true; // Enable/disable timer-based destruction
    public float destroyTime = 3f; // Time in seconds before destruction after player passes
    public float shakeMagnitude = 0.1f; // The maximum magnitude of the shake effect
    public bool enableDistanceDestroy = true; // Enable/disable distance-based destruction
    public float destroyDistance = 8f; // Distance below player to instantly destroy platform
    public float timerDestroyDistance = 4.4f; // Distance below player to start timer-based destruction

    private Transform playerTransform;
    private bool isDestroying = false;
    private float destroyTimer;
    private Vector3 originalPosition;

    private void Start()
    {
        // Find the player by tag
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }

        // Store the original position for the shake effect
        originalPosition = transform.position;
    }

    private void FixedUpdate()
    {
        // If the player hasn't been found yet, try to find it again
        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
            else
            {
                // If player is still not found, do nothing.
                return;
            }
        }

        // Check for distance-based destruction (always active if enabled)
        if (enableDistanceDestroy && playerTransform.position.y > transform.position.y + destroyDistance)
        {
            Destroy(gameObject);
            return;
        }

        // Check if the player has passed the platform (for timer-based destruction)
        if (enableTimerDestroy && !isDestroying && playerTransform.position.y > transform.position.y - timerDestroyDistance)
        {
            isDestroying = true;
            destroyTimer = destroyTime;
        }

        // If the platform is in the process of being destroyed (timer-based)
        if (isDestroying)
        {
            destroyTimer -= Time.fixedDeltaTime;

            if (destroyTimer <= 0f)
            {
                Destroy(gameObject);
            }
            else
            {
                float shakeStartTime = destroyTime * (2.0f / 3.0f);
                if (destroyTimer <= shakeStartTime)
                {
                    // Calculate the progress of the shake (from 0 to 1) over the last 2/3 of the time
                    float shakeProgress = 1f - (destroyTimer / shakeStartTime);
                    float currentShakeMagnitude = shakeMagnitude * shakeProgress;
                    transform.position = originalPosition + Random.insideUnitSphere * currentShakeMagnitude;
                }
                else
                {
                    // If not shaking yet, ensure the position is the original one
                    transform.position = originalPosition;
                }
            }
        }
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerBallController player = collision.gameObject.GetComponent<PlayerBallController>();
        if (player != null)
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

        // Debug information
        // Debug.Log($"Platform Collision - IsOnTop: {isOnTop}, IsNotMovingUp: {isNotMovingUp}, PlayerY: {player.transform.position.y:F2}, PlatformY: {transform.position.y:F2}, VelocityY: {rb.velocity.y:F2}");

        // Jump if player is on top AND not moving upward
        if (isOnTop && isNotMovingUp)
        {
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
                // Add random variance of ±0.1 to make sounds less repetitive
                float pitchVariance = Random.Range(-0.1f, 0.1f);
                float pitch = 1f + pitchVariance; // Base pitch of 1.0 with variance
                SoundEffectsManager.Instance.PlaySound("platform", -1f, pitch);
            }

            //Debug.Log($"Jump Applied - Force: {totalJumpForce:F2}, Base: {jumpForce:F2}, Bonus: {jumpBonus:F2}, RelativeVelocity: {relativeVelocity:F2}");
        }
    }

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
                            //Debug.Log($"Platform Combo Incremented - Velocity: {relativeVelocity:F2}");
                        }
                    }
                }
            }
        }
        catch (System.Exception)
        {
            // ComboManager not available
            //Debug.Log($"ComboManager not found - No combo incremented: {ex.Message}");
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
}
