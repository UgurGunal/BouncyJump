using UnityEngine;

public class PlayerBallController : MonoBehaviour
{
    [Header("Movement")]
    public float acceleration = 45f;
    public float deceleration = 2f;
    public float turnFactor = 90f;
    public float maxSpeed = 6f;
    public float restartMargin = 0f;

    [Header("Combo Speed System")]
    public bool enableComboSpeedSystem = false; // Set to true if you want combo-based speed increase

    [Header("Collision Scale Effects")]
    public bool enableWallCollisionScaleEffect = true; // Enable/disable wall collision scale effects
    public float minCollisionSpeedForEffect = 2f; // Minimum speed to trigger scale effect
    public float maxCollisionSpeedForEffect = 9f; // Maximum speed for full scale effect
    public float minSquishScale = 0.60f; // Minimum X scale (maximum squish) - 0.8 = 20% reduction
    public float maxSquishScale = 0.95f; // Maximum X scale (minimum squish) - 0.95 = 5% reduction
    public float scaleEffectDuration = 0.15f; // Duration of the scale effect in seconds

    [Header("Platform Collision Scale Effects")]
    public bool enablePlatformCollisionScaleEffect = true; // Enable/disable platform collision scale effects
    public float minPlatformCollisionSpeedForEffect = 2f; // Minimum Y speed to trigger platform scale effect
    public float maxPlatformCollisionSpeedForEffect = 9f; // Maximum Y speed for full platform scale effect
    public float minPlatformSquishScale = 0.60f; // Minimum Y scale (maximum squish) - 0.7 = 30% reduction
    public float maxPlatformSquishScale = 0.95f; // Maximum Y scale (minimum squish) - 0.9 = 10% reduction
    public float platformScaleEffectDuration = 0.15f; // Duration of the platform scale effect in seconds

    [Header("Wall Bounce Particles")]
    public ParticleSystem wallDustParticleSystemPrefab; // Particle system prefab to instantiate on each collision (spawns at wall)



    private Rigidbody2D rb;
    private float moveInput = 0f;
    private bool isTouchingSideWall = false;
    private Camera mainCamera;
    private float effectiveMaxSpeed; // Dynamic max speed including combo bonus
    private ComboManager comboManager; // Direct reference instead of reflection
    private bool gameStarted = false; // Track if the 0.5-second delay has passed
    private Vector3 originalScale; // Store the original scale for restoration
    private bool isScaleEffectActive = false; // Track if scale effect is currently active
    private bool isPlatformScaleEffectActive = false; // Track if platform scale effect is currently active
    private TrailRenderer trailRenderer; // Reference to trail renderer component
    private Vector3 originalParticlePosition; // Store original particle system position (from prefab)

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        
        // Get trail renderer component
        trailRenderer = GetComponent<TrailRenderer>();
        
        // Always enable trail emission with dynamic gradient
        if (trailRenderer != null)
        {
            trailRenderer.emitting = true;
            SetupTrailRenderer();
        }
        
        // Store original particle system position from prefab if it exists
        if (wallDustParticleSystemPrefab != null)
        {
            var shape = wallDustParticleSystemPrefab.shape;
            originalParticlePosition = shape.position;
        }
        
        // Store original scale for restoration
        originalScale = transform.localScale;
        
        // Get direct reference to ComboManager
        comboManager = ComboManager.Instance;
        if (comboManager == null)
        {
            comboManager = FindObjectOfType<ComboManager>();
        }
        
        UpdateEffectiveMaxSpeed();
        
        // Start the 0.5-second delay before allowing player movement
        StartCoroutine(StartGameDelay());
    }

    void Update()
    {
        // Only handle input if the game has started (after 0.5-second delay)
        if (gameStarted)
        {
            HandleInput();
        }
        else
        {
            // Reset input to zero when game hasn't started to prevent any movement
            moveInput = 0f;
        }
        
        // Update trail gradient based on current combo
        UpdateTrailGradient();
    }
    
    private void SetupTrailRenderer()
    {
        if (trailRenderer == null) return;
        
        // Set up trail renderer for gradient mode
        trailRenderer.colorGradient = new Gradient();
        
        // Create gradient with white color
        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0] = new GradientColorKey(Color.white, 0f);
        colorKeys[1] = new GradientColorKey(Color.white, 1f);
        
        // Create alpha keys (will be updated dynamically)
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(0f, 0f); // Start with 0 alpha
        alphaKeys[1] = new GradientAlphaKey(0f, 1f); // End with 0 alpha
        
        trailRenderer.colorGradient.SetKeys(colorKeys, alphaKeys);
    }
    
    private void UpdateTrailGradient()
    {
        if (trailRenderer == null || comboManager == null) return;
        
        // Calculate alpha based on combo value with 200 threshold
        // Alpha is 0 when combo < 200, then scales from 0-1 as combo goes from 200-1000
        float currentCombo = comboManager.CurrentCombo;
        float alpha = 0f;
        
        if (currentCombo >= 160f)
        {
            // Map combo range (200-1000) to alpha range (0-1)
            alpha = Mathf.Clamp01((currentCombo - 160f) / 1200f);
        }
        // If combo < 200, alpha remains 0

        Color startColor = new Color(1f, 1f, 1f, alpha); // White with calculated alpha
        Color endColor = new Color(1f, 1f, 1f, 0f); // White with 0 alpha
        
        trailRenderer.startColor = startColor;
        trailRenderer.endColor = endColor;
    }
    
    private void HandleInput()
    {
        
        moveInput = 0f;
        
        #if UNITY_EDITOR || UNITY_STANDALONE
        moveInput = Input.GetAxisRaw("Horizontal");
        #endif

        #if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                float screenMid = Screen.width * 0.5f; // Cached multiplication
                moveInput = touch.position.x < screenMid ? -1f : 1f;
            }
        }
        #endif
    }

    void FixedUpdate()
    {
        // Only allow physics movement if the game has started
        if (!gameStarted) return;

        UpdateEffectiveMaxSpeed();

        // Allow movement when not touching wall, or when moving away from wall
        bool canMove = !isTouchingSideWall || IsMovingAwayFromWall();
        
        if (canMove)
        {
            float currentVelocityX = rb.velocity.x;
            float targetVelocityX = moveInput * effectiveMaxSpeed; // Use effective max speed instead of maxSpeed
            
            if (moveInput != 0f)
            {
                // Check if changing direction
                bool changingDirection = (moveInput > 0f && currentVelocityX < 0f) || (moveInput < 0f && currentVelocityX > 0f);
                float velocityChange = (changingDirection ? turnFactor : acceleration) * Time.fixedDeltaTime;
                
                if (Mathf.Abs(targetVelocityX - currentVelocityX) > velocityChange)
                {
                    currentVelocityX += Mathf.Sign(targetVelocityX - currentVelocityX) * velocityChange;
                }
                else
                {
                    currentVelocityX = targetVelocityX;
                }
            }
            else
            {
                // Decelerate
                float velocityChange = deceleration * Time.fixedDeltaTime;
                if (Mathf.Abs(currentVelocityX) > velocityChange)
                {
                    currentVelocityX -= Mathf.Sign(currentVelocityX) * velocityChange;
                }
                else
                {
                    currentVelocityX = 0f;
                }
            }
            
            rb.velocity = new Vector2(currentVelocityX, rb.velocity.y);
        }
    }

    private void UpdateEffectiveMaxSpeed()
    {
        if (comboManager == null)
        {
            effectiveMaxSpeed = maxSpeed;
            return;
        }
        
        if (enableComboSpeedSystem)
        {
            float bonusSpeed = comboManager.CalculateBonusSpeedLimit();
            effectiveMaxSpeed = maxSpeed + bonusSpeed;
        }
        else
        {
            effectiveMaxSpeed = maxSpeed;
        }
    }



    public void SetTouchingSideWall(bool touching)
    {
        isTouchingSideWall = touching;
    }
    
    private bool IsMovingAwayFromWall()
    {
        // If not touching wall, always allow movement
        if (!isTouchingSideWall) return true;
        
        // Check if player is trying to move away from the wall they're touching
        // This requires knowing which wall we're touching, so let's use a simpler approach:
        // Allow movement if input direction is opposite to current velocity direction
        // or if player is at the edge of the screen and trying to move inward
        
        float screenWidth = Camera.main.orthographicSize * Camera.main.aspect;
        float playerX = transform.position.x;
        
        // If player is on left side and moving right, or on right side and moving left
        if ((playerX < 0 && moveInput > 0) || (playerX > 0 && moveInput < 0))
        {
            return true; // Moving toward center, allow it
        }
        
        // If player has no input, allow natural physics to take over
        if (Mathf.Abs(moveInput) < 0.1f)
        {
            return true;
        }
        
        return false; // Prevent moving further into wall
    }

    public void Jump(float jumpForce)
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        
        // Check landing speed and apply platform scale effect
        float landingSpeed = Mathf.Abs(rb.velocity.y);
        ApplyPlatformScaleEffect(landingSpeed);
    }

    public void BounceFromWall(float bounceForce, float direction)
    {
        rb.velocity = new Vector2(direction * bounceForce, rb.velocity.y);
        
        // Check collision speed and apply scale effect
        float collisionSpeed = Mathf.Abs(rb.velocity.x);
        ApplyScaleEffect(collisionSpeed);
    }

    // Called by SideWall to trigger dust particles on wall collision
    public void TriggerWallDustParticles(SideWall.WallSide wallSide, float collisionSpeed)
    {
        if (wallDustParticleSystemPrefab == null) return;

        // Instantiate a new particle system instance at the player's position
        // Don't parent it so particles are independent of player movement
        ParticleSystem particleInstance = Instantiate(wallDustParticleSystemPrefab, transform.position, Quaternion.identity);
        
        // Get the shape module to adjust position
        var shape = particleInstance.shape;
        
        // Set specific X positions based on wall side
        Vector3 position = originalParticlePosition;
        
        if (wallSide == SideWall.WallSide.Right)
        {
            // Right wall: X position = 0.26
            position.x = 0.23f;
            
            // Also invert the rotation if needed (for cone/circle shapes)
            Vector3 rotation = shape.rotation;
            rotation.z = -rotation.z; // Flip rotation around Z axis
            shape.rotation = rotation;
        }
        else
        {
            // Left wall: X position = -0.28
            position.x = -0.28f;
            
            // Reset rotation for left wall
            Vector3 rotation = shape.rotation;
            rotation.z = Mathf.Abs(rotation.z); // Ensure positive rotation for left wall
            shape.rotation = rotation;
        }
        
        // Apply the position
        shape.position = position;
        
        // Get the texture sheet animation module to flip sprites
        var textureSheetAnimation = particleInstance.textureSheetAnimation;
        
        if (wallSide == SideWall.WallSide.Right)
        {
            // Flip sprites horizontally for right wall
            textureSheetAnimation.flipU = 1f; // 1 = flipped, 0 = not flipped
        }
        else
        {
            // Left wall - keep normal orientation
            textureSheetAnimation.flipU = 0f;
        }
        
        // Calculate particle count based on collision speed
        // Speed 3 = 1 particle, Speed 10+ = 5 particles
        float minSpeed = 8f;
        float maxSpeed = 12f;
        float minParticles = 1f;
        float maxParticles = 5f;
        
        // Clamp speed and calculate particle count
        float clampedSpeed = Mathf.Clamp(collisionSpeed, minSpeed, maxSpeed);
        float speedRatio = (clampedSpeed - minSpeed) / (maxSpeed - minSpeed);
        int particleCount = Mathf.RoundToInt(Mathf.Lerp(minParticles, maxParticles, speedRatio));
        
        // Ensure we have at least 1 particle
        particleCount = Mathf.Max(1, particleCount);
        
        // Make sure the particle system is ready (stop and clear any existing particles)
        particleInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        // Instead of modifying bursts, directly emit the calculated number of particles
        // This is more reliable than trying to modify burst settings
        particleInstance.Emit(particleCount);
        
        // Play the particle system to ensure it's active
        particleInstance.Play();
        
        // Destroy the particle system after it finishes playing
        // Get the maximum lifetime from the main module
        var main = particleInstance.main;
        float maxLifetime = main.startLifetime.constantMax;
        if (maxLifetime <= 0)
        {
            maxLifetime = main.startLifetime.constant; // Fallback to constant if max is 0
        }
        if (maxLifetime <= 0)
        {
            maxLifetime = 2f; // Default fallback
        }
        
        // Destroy after lifetime + small buffer
        Destroy(particleInstance.gameObject, maxLifetime + 0.5f);
    }

    // Public methods to control combo speed system
    public void EnableComboSpeedSystem(bool enable)
    {
        enableComboSpeedSystem = enable;
        // Force immediate update when toggling
        UpdateEffectiveMaxSpeed();
    }

    public float GetEffectiveMaxSpeed()
    {
        return effectiveMaxSpeed;
    }

    private System.Collections.IEnumerator StartGameDelay()
    {
        // Freeze the player's rigidbody for 0.5 seconds
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }
        
        // Wait for 0.5 seconds
        yield return new WaitForSeconds(0.5f);
        
        // Unfreeze the player and allow movement
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        
        gameStarted = true;
        Debug.Log("Game started - Player can now move!");
    }

    private void ApplyScaleEffect(float collisionSpeed)
    {
        // Don't apply effect if disabled
        if (!enableWallCollisionScaleEffect)
            return;

        // Don't apply effect if speed is below minimum threshold
        if (collisionSpeed < minCollisionSpeedForEffect)
            return;

        // Don't apply effect if already active
        if (isScaleEffectActive)
            return;

        // Calculate scale reduction based on collision speed
        float speedRatio = Mathf.Clamp01((collisionSpeed - minCollisionSpeedForEffect) / 
                                        (maxCollisionSpeedForEffect - minCollisionSpeedForEffect));
        
        // Interpolate between max and min squish scale based on speed
        float targetXScale = Mathf.Lerp(maxSquishScale, minSquishScale, speedRatio);
        
        // Apply the scale effect
        Vector3 newScale = originalScale;
        newScale.x = targetXScale;
        transform.localScale = newScale;
        
        // Start the restoration coroutine
        StartCoroutine(RestoreScaleAfterDelay());
        
        float scaleReductionPercent = (1f - targetXScale) * 100f;
    }

    private System.Collections.IEnumerator RestoreScaleAfterDelay()
    {
        isScaleEffectActive = true;
        
        Vector3 squishedScale = transform.localScale; // Current squished scale
        float elapsedTime = 0f;
        
        // Smoothly animate back to original scale over the duration
        while (elapsedTime < scaleEffectDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / scaleEffectDuration;
            
            // Smooth interpolation from squished scale to original scale
            transform.localScale = Vector3.Lerp(squishedScale, originalScale, progress);
            
            yield return null;
        }
        
        // Ensure we end up exactly at the original scale
        transform.localScale = originalScale;
        isScaleEffectActive = false;
    }

    private void ApplyPlatformScaleEffect(float landingSpeed)
    {
        // Don't apply effect if disabled
        if (!enablePlatformCollisionScaleEffect)
            return;

        // Don't apply effect if speed is below minimum threshold
        if (landingSpeed < minPlatformCollisionSpeedForEffect)
            return;

        // Don't apply effect if already active
        if (isPlatformScaleEffectActive)
            return;

        // Calculate scale reduction based on landing speed
        float speedRatio = Mathf.Clamp01((landingSpeed - minPlatformCollisionSpeedForEffect) / 
                                        (maxPlatformCollisionSpeedForEffect - minPlatformCollisionSpeedForEffect));
        
        // Interpolate between max and min squish scale based on speed
        float targetYScale = Mathf.Lerp(maxPlatformSquishScale, minPlatformSquishScale, speedRatio);
        
        // Apply the scale effect
        Vector3 newScale = originalScale;
        newScale.y = targetYScale;
        transform.localScale = newScale;
        
        // Start the restoration coroutine
        StartCoroutine(RestorePlatformScaleAfterDelay());
        
        float scaleReductionPercent = (1f - targetYScale) * 100f;
    }

    private System.Collections.IEnumerator RestorePlatformScaleAfterDelay()
    {
        isPlatformScaleEffectActive = true;
        
        Vector3 squishedScale = transform.localScale; // Current squished scale
        float elapsedTime = 0f;
        
        // Smoothly animate back to original scale over the duration
        while (elapsedTime < platformScaleEffectDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / platformScaleEffectDuration;
            
            // Smooth interpolation from squished scale to original scale
            transform.localScale = Vector3.Lerp(squishedScale, originalScale, progress);
            
            yield return null;
        }
        
        // Ensure we end up exactly at the original scale
        transform.localScale = originalScale;
        isPlatformScaleEffectActive = false;
    }

    public void Revive(Vector2 revivePosition)
    {
        // Reset position
        transform.position = revivePosition;
        
        // Reset velocity
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        
        // Clear trail renderer to prevent long trail from teleportation
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
        
        // Reset any other player state as needed
        isTouchingSideWall = false;
        
        //Debug.Log($"Player revived at position: {revivePosition}");
    }
}