using UnityEngine;

public class SideWall : MonoBehaviour
{
    public enum WallSide { Left, Right }
    public enum BounceForceMode { Linear, Quadratic, Exponential, Custom }
    
    public WallSide wallSide;
    public BounceForceMode bounceForceMode = BounceForceMode.Linear;
    
    [Header("Linear Mode Settings")]
    public float minForce = 2f;
    public float maxForce = 12f;
    public float playerMaxSpeed = 12f; // Should match the player's maxSpeed
    
    [Header("Quadratic Mode Settings")]
    public float quadraticDivisor = 4f; // For speed^2/4 calculation
    
    [Header("Exponential Mode Settings")]
    public float exponentialBase = 1.5f; // Base for exponential growth
    public float exponentialMultiplier = 2f; // Multiplier for exponential calculation
    
    [Header("Custom Mode Settings")]
    public float customMinForce = 2f;
    public float customMaxForce = 15f;
    public float customGrowthRate = 0.8f; // Between linear (0) and quadratic (1)

    [Header("Combo System")]
    public bool enableComboSystem = false; // Set to true if you want combo functionality

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerBallController player = collision.gameObject.GetComponent<PlayerBallController>();
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            
            // Use relative velocity from collision for more accurate speed calculation
            float playerSpeed = Mathf.Abs(collision.relativeVelocity.x);
            
            // Debug: Show relative velocity details
            //Debug.Log($"WallCollision Velocity: {collision.relativeVelocity}");

            // Calculate force based on selected mode
            float bounceForce = CalculateBounceForce(playerSpeed);
            
            // Determine bounce direction
            float bounceDirection = (wallSide == WallSide.Left) ? 1f : -1f;
            
            // Apply bounce using player's method
            player.BounceFromWall(bounceForce, bounceDirection);
            
            // Add combo from wall bounce (optional)
            if (enableComboSystem)
            {
                AddWallCombo(playerSpeed);
            }
            
            player.SetTouchingSideWall(true);
        }
    }

    private float CalculateBounceForce(float playerSpeed)
    {
        switch (bounceForceMode)
        {
            case BounceForceMode.Linear:
                return CalculateLinearBounceForce(playerSpeed);
                
            case BounceForceMode.Quadratic:
                return CalculateQuadraticBounceForce(playerSpeed);
                
            case BounceForceMode.Exponential:
                return CalculateExponentialBounceForce(playerSpeed);
                
            case BounceForceMode.Custom:
                return CalculateCustomBounceForce(playerSpeed);
                
            default:
                return CalculateLinearBounceForce(playerSpeed);
        }
    }

    private float CalculateLinearBounceForce(float playerSpeed)
    {
        // Original linear calculation: speed ratio between min and max force
        float speedRatio = Mathf.Clamp01(playerSpeed / playerMaxSpeed);
        return Mathf.Lerp(minForce, maxForce, speedRatio);
    }

    private float CalculateQuadraticBounceForce(float playerSpeed)
    {
        // Quadratic calculation: speed^2 / divisor
        return (playerSpeed * playerSpeed) / quadraticDivisor;
    }

    private float CalculateExponentialBounceForce(float playerSpeed)
    {
        // Exponential calculation: exponentialBase^(speed/maxSpeed) * multiplier
        float normalizedSpeed = playerSpeed / playerMaxSpeed;
        return Mathf.Pow(exponentialBase, normalizedSpeed) * exponentialMultiplier;
    }

    private float CalculateCustomBounceForce(float playerSpeed)
    {
        // Custom growth between linear and quadratic
        // Uses a power function where customGrowthRate controls the curve
        // 0 = linear, 1 = quadratic, 0.5 = between linear and quadratic
        float normalizedSpeed = playerSpeed / playerMaxSpeed;
        float powerValue = Mathf.Pow(normalizedSpeed, 1f + customGrowthRate);
        return Mathf.Lerp(customMinForce, customMaxForce, powerValue);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        PlayerBallController player = collision.gameObject.GetComponent<PlayerBallController>();
        if (player != null)
        {
            player.SetTouchingSideWall(false);
        }
    }

    private void AddWallCombo(float playerSpeed)
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
                        var wallComboMethod = comboManagerType.GetMethod("WallComboIncrement");
                        if (wallComboMethod != null)
                        {
                            wallComboMethod.Invoke(instance, new object[] { gameObject, playerSpeed });
                            //Debug.Log($"ComboManager found - Added wall bounce combo");
                        }
                    }
                }
            }
        }
        catch (System.Exception)
        {
            // ComboManager not available
            //Debug.Log($"ComboManager not found - No combo added for wall bounce");
        }
    }

    // Public methods to control combo system manually
    public void EnableComboSystem(bool enable)
    {
        enableComboSystem = enable;
    }

    // Public method to change bounce force mode at runtime
    public void SetBounceForceMode(BounceForceMode mode)
    {
        bounceForceMode = mode;
    }
} 