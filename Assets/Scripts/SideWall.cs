using UnityEngine;

public class SideWall : MonoBehaviour
{
    public enum WallSide { Left, Right }
    
    [Header("Wall Settings")]
    public WallSide wallSide;
    public float minForce = 1f;
    public float maxForce = 8f;
    public float playerMaxSpeed = 10f; // Should match the player's maxSpeed

    [Header("Combo System")]
    public bool enableComboSystem = true; // Set to true if you want combo functionality
    public float wallCooldownDuration = 1.5f; // Public variable to manage cooldown duration

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerBallController player = collision.gameObject.GetComponent<PlayerBallController>();
        if (player != null)
        {
            // Use relative velocity from collision for more accurate speed calculation
            float playerSpeed = Mathf.Abs(collision.relativeVelocity.x);

            // Calculate bounce force using linear interpolation
            float bounceForce = CalculateBounceForce(playerSpeed);
            
            // Determine bounce direction
            float bounceDirection = (wallSide == WallSide.Left) ? 1f : -1f;
            
            // Apply bounce using player's method
            player.BounceFromWall(bounceForce, bounceDirection);
            
            // Add combo from wall bounce with custom cooldown logic
            if (enableComboSystem)
            {
                AddWallComboWithCooldown(playerSpeed);
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
        PlayerBallController player = collision.gameObject.GetComponent<PlayerBallController>();
        if (player != null)
        {
            player.SetTouchingSideWall(false);
        }
    }

    private void AddWallComboWithCooldown(float playerSpeed)
    {
        // Try to add combo safely without direct ComboManager reference
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
                        // Call the custom wall combo method with cooldown management
                        var wallComboMethod = comboManagerType.GetMethod("WallComboIncrementWithCooldown");
                        if (wallComboMethod != null)
                        {
                            wallComboMethod.Invoke(instance, new object[] { gameObject, playerSpeed, wallCooldownDuration });
                        }
                        else
                        {
                            // Fallback to original method if custom method doesn't exist
                            var originalMethod = comboManagerType.GetMethod("WallComboIncrement");
                            if (originalMethod != null)
                            {
                                originalMethod.Invoke(instance, new object[] { gameObject, playerSpeed });
                            }
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
} 