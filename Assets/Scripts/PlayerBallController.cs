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



    private Rigidbody2D rb;
    private float moveInput = 0f;
    private bool isTouchingSideWall = false;
    private Camera mainCamera;
    private float effectiveMaxSpeed; // Dynamic max speed including combo bonus
    private ComboManager comboManager; // Direct reference instead of reflection
    private bool gameStarted = false; // Track if the 0.5-second delay has passed

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        
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

        if (!isTouchingSideWall)
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

    public void Jump(float jumpForce)
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    public void BounceFromWall(float bounceForce, float direction)
    {
        rb.velocity = new Vector2(direction * bounceForce, rb.velocity.y);
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

    public void Revive(Vector2 revivePosition)
    {
        // Reset position
        transform.position = revivePosition;
        
        // Reset velocity
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        
        // Reset any other player state as needed
        isTouchingSideWall = false;
        
        //Debug.Log($"Player revived at position: {revivePosition}");
    }
}