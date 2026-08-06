using UnityEngine;

public class PlayerBallController : MonoBehaviour
{
    public static PlayerBallController Instance { get; private set; }

    /// <summary>Fired while the player is steering left (-1) or right (+1).</summary>
    public static event System.Action<float> OnDirectionalInput;

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

    [Header("Particles")]
    [Tooltip("Handles wall dust and player bounce particles. Optional, but recommended.")]
    public PlayerParticleController particleController;
    private Rigidbody2D rb;
    private float moveInput = 0f;
    private bool isTouchingSideWall = false;
    private SideWall.WallSide touchingWallSide;
    /// <summary>Wall contact is refreshed by SideWall Stay; cleared if Stay stops (missed Exit on slow devices).</summary>
    private float wallContactExpireFixedTime = float.NegativeInfinity;
    private Camera mainCamera;
    private float effectiveMaxSpeed; // Dynamic max speed including combo bonus
    private ComboManager comboManager; // Direct reference instead of reflection
    private bool gameStarted = false; // Track if the 0.5-second delay has passed
    private Vector3 originalScale; // Store the original scale for restoration
    private bool isScaleEffectActive = false; // Track if scale effect is currently active
    private bool isPlatformScaleEffectActive = false; // Track if platform scale effect is currently active
    private TrailRenderer trailRenderer; // Reference to trail renderer component

    static readonly Color PowerupTrailColor = new Color(0f, 0xBB / 255f, 1f);

    public Rigidbody2D Rigidbody
    {
        get
        {
            if (rb == null)
                rb = GetComponent<Rigidbody2D>();
            return rb;
        }
    }

    void Awake()
    {
        Instance = this;
        GameplayPlayerCache.SetPlayer(transform);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

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

        bool powerupActive = comboManager.IsPowerupActive;
        Color trailRgb = powerupActive ? PowerupTrailColor : Color.white;
        if (powerupActive)
            alpha = 1f;

        Color startColor = new Color(trailRgb.r, trailRgb.g, trailRgb.b, alpha);
        Color endColor = new Color(trailRgb.r, trailRgb.g, trailRgb.b, 0f);
        
        trailRenderer.startColor = startColor;
        trailRenderer.endColor = endColor;
    }
    
    private void HandleInput()
    {
        moveInput = 0f;

        #if UNITY_EDITOR || UNITY_STANDALONE
        // Keyboard/arrows: left = -1, right = +1 (same roles as mobile screen halves).
        moveInput = Input.GetAxisRaw("Horizontal");
        #endif

        #if UNITY_ANDROID || UNITY_IOS
        // Half-screen steer. Prefer touches; fall back to mouse (some older Android stacks expose the
        // primary finger only via mouse simulation, or drop GetTouch(0) while another finger is held).
        if (TryGetSteeringPointerX(out float pointerX))
        {
            float screenMid = Screen.width * 0.5f;
            moveInput = pointerX < screenMid ? -1f : 1f;
        }
        #endif

        if (!Mathf.Approximately(moveInput, 0f))
            OnDirectionalInput?.Invoke(moveInput);
    }

    /// <summary>
    /// Resolves the active pointer X in screen pixels for left/right steering.
    /// </summary>
    private static bool TryGetSteeringPointerX(out float pointerX)
    {
        pointerX = 0f;

        int touchCount = Input.touchCount;
        for (int i = 0; i < touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began ||
                touch.phase == TouchPhase.Moved ||
                touch.phase == TouchPhase.Stationary)
            {
                pointerX = touch.position.x;
                return true;
            }
        }

        // Mouse fallback also covers Editor device simulation and OEM stacks that mirror touch as mouse.
        if (Input.GetMouseButton(0))
        {
            pointerX = Input.mousePosition.x;
            return true;
        }

        return false;
    }

    void FixedUpdate()
    {
        // Only allow physics movement if the game has started
        if (!gameStarted) return;

        // Clear stale wall contact when Exit was missed (common on low-FPS / large physics steps).
        if (isTouchingSideWall && Time.fixedTime > wallContactExpireFixedTime)
            isTouchingSideWall = false;

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
        if (!touching)
            wallContactExpireFixedTime = float.NegativeInfinity;
    }

    /// <summary>
    /// Called from SideWall Enter/Stay so contact stays fresh even if Exit is skipped.
    /// </summary>
    public void NotifySideWallContact(SideWall.WallSide wallSide)
    {
        isTouchingSideWall = true;
        touchingWallSide = wallSide;
        // Stay must refresh within a couple of physics steps or contact is considered cleared.
        wallContactExpireFixedTime = Time.fixedTime + Time.fixedDeltaTime * 2.5f;
    }
    
    private bool IsMovingAwayFromWall()
    {
        if (!isTouchingSideWall) return true;

        // No steer: let bounce / physics resolve without forcing X velocity.
        if (Mathf.Abs(moveInput) < 0.1f)
            return true;

        // Block only pressing further into the wall we are actually contacting.
        if (touchingWallSide == SideWall.WallSide.Left && moveInput < 0f)
            return false;
        if (touchingWallSide == SideWall.WallSide.Right && moveInput > 0f)
            return false;

        return true;
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
        if (particleController != null)
        {
            particleController.TriggerWallDustParticles(wallSide, collisionSpeed);
        }
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
        SetTouchingSideWall(false);
    }
}
