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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerBallController player = collision.gameObject.GetComponent<PlayerBallController>();
        if (player != null)
        {
            // Use relative velocity from collision for more accurate speed calculation
            float playerSpeed = Mathf.Abs(collision.relativeVelocity.x);

            // Check if this wall can give combo (for trail effect)
            bool canGiveCombo = CanWallGiveCombo();
            
            // Only apply bounce force if player speed is above minimum threshold
            if (playerSpeed >= minSpeedForBounce)
            {
                // Calculate bounce force using linear interpolation
                float bounceForce = CalculateBounceForce(playerSpeed);
                
                // Determine bounce direction
                float bounceDirection = (wallSide == WallSide.Left) ? 1f : -1f;
                
                if (!canGiveCombo){
                    bounceForce = bounceForce * 0.7f;
                }
                // Apply bounce using player's method
                player.BounceFromWall(bounceForce, bounceDirection);
            }
            
            
            // Add combo from wall bounce with custom cooldown logic
            bool comboAdded = false;
            if (enableComboSystem)
            {
                comboAdded = AddWallComboWithCooldown(playerSpeed);
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

    private bool AddWallComboWithCooldown(float playerSpeed)
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
                        // Check which wall combo system is enabled
                        var useWallCooldownProperty = comboManagerType.GetField("useWallCooldownSystem");
                        var useAlternatingProperty = comboManagerType.GetField("useAlternatingWallCombo");
                        
                        bool useWallCooldown = false;
                        bool useAlternating = false;
                        
                        if (useWallCooldownProperty != null)
                            useWallCooldown = (bool)useWallCooldownProperty.GetValue(instance);
                        if (useAlternatingProperty != null)
                            useAlternating = (bool)useAlternatingProperty.GetValue(instance);
                        
                        if (useWallCooldown)
                        {
                            // Check if wall is on cooldown BEFORE calling the method
                            bool wasOnCooldown = IsWallOnCooldown();
                            
                            if (wasOnCooldown)
                            {
                                // Wall is on cooldown - reset opposite wall's cooldown but don't reset this wall's cooldown
                                var resetOppositeMethod = comboManagerType.GetMethod("ResetOppositeWallCooldown");
                                
                                if (resetOppositeMethod != null)
                                {
                                    // Reset opposite wall's cooldown
                                    resetOppositeMethod.Invoke(instance, new object[] { wallSide });
                                }
                                
                                return false; // No combo added, but opposite wall cooldown is reset
                            }
                            else
                            {
                                // Wall is not on cooldown - give combo
                                var individualCooldownMethod = comboManagerType.GetMethod("WallComboIncrementWithIndividualCooldown");
                                if (individualCooldownMethod != null)
                                {
                                    individualCooldownMethod.Invoke(instance, new object[] { gameObject, playerSpeed, wallCooldownDuration });
                                    return true; // Combo was added
                                }
                            }
                        }
                        else if (useAlternating)
                        {
                            // Use the alternating wall combo method
                            var alternatingMethod = comboManagerType.GetMethod("WallComboIncrementAlternating");
                            if (alternatingMethod != null)
                            {
                                alternatingMethod.Invoke(instance, new object[] { gameObject, playerSpeed });
                                return true; // Alternating system always gives combo when called
                            }
                        }
                        else
                        {
                            // Use the original cooldown-based method
                            var wallComboMethod = comboManagerType.GetMethod("WallComboIncrementWithCooldown");
                            if (wallComboMethod != null)
                            {
                                wallComboMethod.Invoke(instance, new object[] { gameObject, playerSpeed, wallCooldownDuration });
                                return true; // Original system always gives combo when called
                            }
                            else
                            {
                                // Fallback to original method if custom method doesn't exist
                                var originalMethod = comboManagerType.GetMethod("WallComboIncrement");
                                if (originalMethod != null)
                                {
                                    originalMethod.Invoke(instance, new object[] { gameObject, playerSpeed });
                                    return true; // Fallback always gives combo when called
                                }
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
        
        return false; // Default to no combo if we can't determine the state
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
    
    // Check if this wall can give combo (for trail effect)
    private bool CanWallGiveCombo()
    {
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
                        // Check which wall combo system is enabled
                        var useWallCooldownProperty = comboManagerType.GetField("useWallCooldownSystem");
                        var useAlternatingProperty = comboManagerType.GetField("useAlternatingWallCombo");
                        
                        bool useWallCooldown = false;
                        bool useAlternating = false;
                        
                        if (useWallCooldownProperty != null)
                            useWallCooldown = (bool)useWallCooldownProperty.GetValue(instance);
                        if (useAlternatingProperty != null)
                            useAlternating = (bool)useAlternatingProperty.GetValue(instance);
                        
                        if (useWallCooldown)
                        {
                            // Check if this wall is on cooldown
                            if (wallSide == WallSide.Left)
                            {
                                var isLeftOnCooldownMethod = comboManagerType.GetMethod("IsLeftWallOnCooldown");
                                if (isLeftOnCooldownMethod != null)
                                {
                                    return !(bool)isLeftOnCooldownMethod.Invoke(instance, null);
                                }
                            }
                            else if (wallSide == WallSide.Right)
                            {
                                var isRightOnCooldownMethod = comboManagerType.GetMethod("IsRightWallOnCooldown");
                                if (isRightOnCooldownMethod != null)
                                {
                                    return !(bool)isRightOnCooldownMethod.Invoke(instance, null);
                                }
                            }
                        }
                        else if (useAlternating)
                        {
                            // Get the current wall combo state
                            var getStateMethod = comboManagerType.GetMethod("GetCurrentWallComboState");
                            if (getStateMethod != null)
                            {
                                var currentState = getStateMethod.Invoke(instance, null);
                                
                                // Check if this wall can give combo based on current state
                                if (currentState != null)
                                {
                                    string stateName = currentState.ToString();
                                    
                                    if (stateName == "BothActive")
                                        return true; // Both walls can give combo
                                    else if (stateName == "LeftOnly")
                                        return wallSide == WallSide.Left; // Only left wall can give combo
                                    else if (stateName == "RightOnly")
                                        return wallSide == WallSide.Right; // Only right wall can give combo
                                }
                            }
                        }
                        else
                        {
                            // If both systems are disabled, all walls can give combo
                            return true;
                        }
                    }
                }
            }
        }
        catch (System.Exception)
        {
            // ComboManager not available, assume wall can give combo
        }
        
        return false; // Default to no combo if we can't determine the state
    }
    
    // Check if this wall is currently on cooldown
    private bool IsWallOnCooldown()
    {
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
                        // Check if wall cooldown system is enabled
                        var useWallCooldownProperty = comboManagerType.GetField("useWallCooldownSystem");
                        bool useWallCooldown = false;
                        
                        if (useWallCooldownProperty != null)
                            useWallCooldown = (bool)useWallCooldownProperty.GetValue(instance);
                        
                        if (useWallCooldown)
                        {
                            // Check if this wall is on cooldown
                            if (wallSide == WallSide.Left)
                            {
                                var isLeftOnCooldownMethod = comboManagerType.GetMethod("IsLeftWallOnCooldown");
                                if (isLeftOnCooldownMethod != null)
                                {
                                    return (bool)isLeftOnCooldownMethod.Invoke(instance, null);
                                }
                            }
                            else if (wallSide == WallSide.Right)
                            {
                                var isRightOnCooldownMethod = comboManagerType.GetMethod("IsRightWallOnCooldown");
                                if (isRightOnCooldownMethod != null)
                                {
                                    return (bool)isRightOnCooldownMethod.Invoke(instance, null);
                                }
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
        
        return false; // Default to not on cooldown if we can't determine the state
    }
} 