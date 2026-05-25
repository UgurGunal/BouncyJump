using UnityEngine;

public class SideWall : MonoBehaviour
{
    public enum WallSide { Left, Right }
    
    [Header("Wall Settings")]
    public WallSide wallSide;
    public float minForce = 1f;
    public float maxForce = 8f;
    public float playerMaxSpeed = 9f; // Should match the player's maxSpeed
    private float minSpeedForBounce = 0f; // Minimum speed required to get bounce force

    [Header("Combo System")]
    public bool enableComboSystem = true; // Set to true if you want combo functionality
    private float wallCooldownDuration = 0.8f; // Public variable to manage cooldown duration

    [Header("Punish System")]
    private float punishTimeWindow = 0.3f; // Time window in seconds to trigger punish (restart cooldown)
    private float cooldownStartTime = -1f; // Track when this wall entered cooldown (-1 means not on cooldown)
    private bool wasOnCooldownLastFrame = false; // Track cooldown state from previous frame

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        PlayerBallController player = PlayerBallController.Instance;
        if (player == null)
            player = collision.gameObject.GetComponent<PlayerBallController>();

        if (player != null)
        {
            // Use relative velocity from collision for more accurate speed calculation
            float playerSpeed = Mathf.Abs(collision.relativeVelocity.x);
            float collisionSpeed = collision.relativeVelocity.magnitude;

            // Check if wall is currently on cooldown (before trying to add combo)
            bool isOnCooldown = IsWallOnCooldown();
            
            // Check punish system: if wall is on cooldown and hit within punish window, restart cooldown
            if (isOnCooldown && cooldownStartTime >= 0f)
            {
                float timeSinceCooldownStart = Time.time - cooldownStartTime;
                if (timeSinceCooldownStart < punishTimeWindow)
                {
                    // Restart the cooldown timer
                    RestartWallCooldown();
                    // Don't add combo, just apply reduced bounce force
                    if (playerSpeed >= minSpeedForBounce)
                    {
                        float bounceForce = CalculateBounceForce(playerSpeed) * 0.4f;
                        float bounceDirection = (wallSide == WallSide.Left) ? 1f : -1f;
                        player.BounceFromWall(bounceForce, bounceDirection);
                    }
                    player.SetTouchingSideWall(true);
                    return; // Exit early, don't process combo addition
                }
            }
            
            // Track cooldown state before trying to add combo
            bool wasOnCooldownBefore = isOnCooldown;
            
            // Add combo from wall bounce with custom cooldown logic (check this first)
            bool comboAdded = false;
            if (enableComboSystem)
            {
                comboAdded = AddWallComboWithCooldown(playerSpeed);
            }
            
            // Track when cooldown starts (when combo was added and wall goes into cooldown)
            bool isOnCooldownAfter = IsWallOnCooldown();
            if (!wasOnCooldownBefore && isOnCooldownAfter)
            {
                // Wall just entered cooldown (combo was added), record the time
                cooldownStartTime = Time.time;
            }
            
            // Only apply bounce force if player speed is above minimum threshold
            if (playerSpeed >= minSpeedForBounce)
            {
                // Calculate bounce force using linear interpolation
                float bounceForce = CalculateBounceForce(playerSpeed);
                
                // Determine bounce direction
                float bounceDirection = (wallSide == WallSide.Left) ? 1f : -1f;
                
                // Reduce bounce force if combo was NOT actually added
                if (!comboAdded){
                    bounceForce = bounceForce * 0.4f;
                }
                // Apply bounce using player's method
                player.BounceFromWall(bounceForce, bounceDirection);
            }
            
            // Only trigger dust particles if combo was actually added
            if (comboAdded)
            {
                player.TriggerWallDustParticles(wallSide, playerSpeed);
                
                // Play wall bounce sound effect when combo is added from wall bounce
                // Pitch and volume shift based on collision speed: lerp between speed 6 and speed 16
                if (SoundEffectsManager.Instance != null)
                {
                    float minSpeed = 6f;
                    float maxSpeed = 16f;
                    float minPitch = 0.9f;
                    float maxPitch = 1.2f;
                    float minVolume = 0.8f;
                    float maxVolume = 0.1f;
                    
                    // Calculate pitch and volume based on collision speed
                    float speedRatio = Mathf.Clamp01((collisionSpeed - minSpeed) / (maxSpeed - minSpeed));
                    float basePitch = Mathf.Lerp(minPitch, maxPitch, speedRatio);
                    float volume = Mathf.Lerp(minVolume, maxVolume, speedRatio);
                    
                    // Add random variance of Â±0.1 to make sounds less repetitive
                    float pitchVariance = Random.Range(-0.1f, 0.1f);
                    float pitch = basePitch + pitchVariance;
                    
                    SoundEffectsManager.Instance.PlaySound("wall", volume, pitch);
                }
            }
            
            player.SetTouchingSideWall(true);
        }
    }

    private float CalculateBounceForce(float playerSpeed)
    {
        // Linear calculation: speed ratio between min and max force
        float speedRatio = Mathf.Clamp01(playerSpeed / playerMaxSpeed);
        return Mathf.Lerp(minForce, maxForce, speedRatio);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        PlayerBallController player = PlayerBallController.Instance;
        if (player == null)
            player = collision.gameObject.GetComponent<PlayerBallController>();

        if (player != null)
            player.SetTouchingSideWall(false);
    }

    private bool AddWallComboWithCooldown(float playerSpeed)
    {
        ComboManager combo = ComboManager.Instance;
        if (combo == null)
            return false;

        if (combo.useWallCooldownSystem)
        {
            if (IsWallOnCooldown())
            {
                combo.ResetOppositeWallCooldown(wallSide);
                return false;
            }

            combo.WallComboIncrementWithIndividualCooldown(gameObject, playerSpeed, wallCooldownDuration);
            return true;
        }

        if (combo.useAlternatingWallCombo)
        {
            combo.WallComboIncrementAlternating(gameObject, playerSpeed);
            return true;
        }

        combo.WallComboIncrementWithCooldown(gameObject, playerSpeed, wallCooldownDuration);
        return true;
    }

    // Public methods to control combo system manually
    public void EnableComboSystem(bool enable)
    {
        enableComboSystem = enable;
    }

    // Public method to change cooldown duration at runtime
    public void SetCooldownDuration(float duration)
    {
        wallCooldownDuration = Mathf.Max(0f, duration);
    }
    
    private bool IsWallOnCooldown()
    {
        ComboManager combo = ComboManager.Instance;
        if (combo == null || !combo.useWallCooldownSystem)
            return false;

        return wallSide == WallSide.Left
            ? combo.IsLeftWallOnCooldown()
            : combo.IsRightWallOnCooldown();
    }

    private void RestartWallCooldown()
    {
        ComboManager combo = ComboManager.Instance;
        if (combo == null || !combo.useWallCooldownSystem)
            return;

        combo.SetWallIndividualCooldownWithDuration(wallSide, wallCooldownDuration);
        cooldownStartTime = Time.time;
    }

    void Update()
    {
        // Reset cooldown tracking when wall is no longer on cooldown
        bool isOnCooldown = IsWallOnCooldown();
        if (!isOnCooldown && wasOnCooldownLastFrame)
        {
            // Wall just exited cooldown
            cooldownStartTime = -1f;
        }
        wasOnCooldownLastFrame = isOnCooldown;
    }
} 
