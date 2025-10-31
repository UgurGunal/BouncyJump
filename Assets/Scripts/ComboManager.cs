using UnityEngine;
using System.Collections.Generic;

public class ComboManager : MonoBehaviour
{
    [Header("Combo Settings")]
    public float maxCombo = 1000f;
    [SerializeField] private float currentCombo = 0f; // Made private with SerializeField for inspector visibility
    public float minDecrease = 110f; // Minimum decay rate per second
    public float maxDecrease = 300f; // Maximum decay rate per second
    
    [Header("Bonus Speed Limit")]
    public float speedLimitBonus = 3f; // Additional speed at max combo
    
    [Header("Ball Rotation")]
    public float rotationSpeedMultiplier = 2f; // Multiplier for combo-based rotation
    
    [Header("Platform Combo Settings")]
    public float platformVelocityMultiplier = 2f; // Multiplier for relative velocity
    public float platformMinimumBonus = 30f; // Minimum bonus value
    public float platformComboMultiplier = 1.00f; // Multiplier for current combo
    
    [Header("Wall Combo Settings")]
    public float wallVelocityMultiplier = 12f; // Multiplier for relative velocity
    public float wallComboMultiplier = 1f; // Multiplier for current combo

    [Header("Wall Cooldown System")]
    public bool useWallCooldownSystem = true; // Enable/disable individual wall cooldown system
    public float leftWallCooldown = 0f; // Current cooldown for left wall
    public float rightWallCooldown = 0f; // Current cooldown for right wall
    public float wallCooldownDuration = 1.5f; // Duration of wall cooldown

    [Header("Alternating Wall Combo System")]
    public bool useAlternatingWallCombo = false; // Disabled by default, using cooldown system instead
    public enum WallComboState { BothActive, LeftOnly, RightOnly }
    private WallComboState currentWallComboState = WallComboState.BothActive;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private PlayerBallController playerController;
    private Dictionary<GameObject, float> wallCooldowns = new Dictionary<GameObject, float>();
    private List<GameObject> expiredCooldowns = new List<GameObject>(); // Reuse list to avoid allocations
    
    // Performance optimization variables
    private float lastDecayUpdate = 0f;
    private const float DECAY_UPDATE_INTERVAL = 0.05f; // Update decay every 0.05 seconds (20 times per second)
    private float lastCooldownUpdate = 0f;
    private const float COOLDOWN_UPDATE_INTERVAL = 0.1f; // Update cooldowns every 0.1 seconds
    
    public static ComboManager Instance { get; private set; }
    
    // Public property for safe access to currentCombo
    public float CurrentCombo => currentCombo;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        playerController = FindObjectOfType<PlayerBallController>();
        
        if (playerController == null && showDebugLogs)
        {
            //Debug.LogWarning("ComboManager: PlayerBallController not found in scene!");
        }
        
        // Initialize combo to 0
        currentCombo = 0f;
        
        if (showDebugLogs)
        {
            //Debug.Log($"ComboManager initialized. Max Combo: {maxCombo}");
        }
    }
    
    void Update()
    {
        // Optimize combo decay - don't update every frame
        if (Time.time - lastDecayUpdate >= DECAY_UPDATE_INTERVAL)
        {
            DecayCombo();
            lastDecayUpdate = Time.time;
        }
        
        // Optimize wall cooldown updates - less frequent than decay
        if (Time.time - lastCooldownUpdate >= COOLDOWN_UPDATE_INTERVAL)
        {
            UpdateWallCooldowns();
            lastCooldownUpdate = Time.time;
        }
    }
    
    private void DecayCombo()
    {
        if (currentCombo <= 0f) return; // Early exit if no combo to decay
        
        float comboRatio = currentCombo / maxCombo;
        float decayRate = Mathf.Lerp(minDecrease, maxDecrease, comboRatio);
        float oldCombo = currentCombo;
        
        // Use the actual time since last update for more accurate decay
        float deltaTime = Time.time - lastDecayUpdate;
        currentCombo -= decayRate * deltaTime;
        currentCombo = Mathf.Max(0f, currentCombo);
        
        if (showDebugLogs && oldCombo > 0f && currentCombo == 0f)
        {
            //Debug.Log("Combo decayed to zero");
        }
    }
    
    public float getCombo()
    {
        return currentCombo;
    }
    
    public float getRotationSpeedMultiplier()
    {
        return rotationSpeedMultiplier;
    }
    
    public float CalculateBonusSpeedLimit()
    {
        if (maxCombo <= 0f) return 0f; // Prevent division by zero
        
        float comboRatio = currentCombo / maxCombo;
        float bonusSpeed = comboRatio * speedLimitBonus;
        
        //if (showDebugLogs && bonusSpeed > 0f)
        //{
        //    Debug.Log($"Speed bonus: {bonusSpeed:F2} (Combo: {currentCombo:F1}/{maxCombo})");
        //}
        
        return bonusSpeed;
    }
    
    public void PlatformComboIncrement(float relativeVelocity)
    {
        if (relativeVelocity < 0f)
        {
            if (showDebugLogs)
            {
                //Debug.LogWarning($"Negative relative velocity for platform: {relativeVelocity}");
            }
            return;
        }
        
        // Calculate bonus as relative velocity Y × platformVelocityMultiplier
        float comboToAdd = relativeVelocity * platformVelocityMultiplier;
        
        // Use the larger of calculated bonus or minimum bonus
        float calculatedCombo = Mathf.Max(comboToAdd, platformMinimumBonus);
        
        float oldCombo = currentCombo;
        
        // Add base combo from platform jump
        AddCombo(calculatedCombo);
        
        // Multiply current combo by platformComboMultiplier
        if (platformComboMultiplier != 1.0f)
        {
            currentCombo *= platformComboMultiplier;
            currentCombo = Mathf.Min(maxCombo, currentCombo);
        }
        
        if (showDebugLogs)
        {
            //Debug.Log($"Platform combo: {oldCombo:F1} → {currentCombo:F1} (Added: {calculatedCombo:F1}, Velocity: {relativeVelocity:F2})");
        }
    }
    
    public void WallComboIncrement(GameObject wall, float relativeVelocity)
    {
        if (wall == null)
        {
            if (showDebugLogs)
                //Debug.LogWarning("WallComboIncrement called with null wall!");
            return;
        }
        
        // Use absolute value to handle negative speeds properly
        float absRelativeVelocity = Mathf.Abs(relativeVelocity);
        
        if (absRelativeVelocity <= 0f)
        {
            if (showDebugLogs)
                //Debug.LogWarning($"Zero or negative relative velocity for wall: {relativeVelocity}");
            return;
        }
        
        // Check if this wall is on cooldown
        if (IsWallOnCooldown(wall))
        {
            if (showDebugLogs)
                //Debug.Log($"Wall {wall.name} is on cooldown, no combo added");
            return;
        }
        
        // Calculate combo as absolute relative velocity × wallVelocityMultiplier
        float calculatedCombo = absRelativeVelocity * wallVelocityMultiplier;
        
        float oldCombo = currentCombo;
        
        // Add base combo from wall bounce
        AddCombo(calculatedCombo);
        
        // Multiply current combo by wallComboMultiplier
        if (wallComboMultiplier != 1.0f)
        {
            currentCombo *= wallComboMultiplier;
            currentCombo = Mathf.Min(maxCombo, currentCombo);
        }
        
        // Set cooldown for this wall and reset others
        SetWallCooldown(wall);
        ResetOtherWallCooldowns(wall);
        
        if (showDebugLogs)
        {
            //Debug.Log($"Wall combo: {oldCombo:F1} → {currentCombo:F1} (Added: {calculatedCombo:F1}, Velocity: {absRelativeVelocity:F2})");
        }
    }

    // New method with custom cooldown logic: hitting one wall resets others and sets current to max cooldown
    public void WallComboIncrementWithCooldown(GameObject wall, float relativeVelocity, float customCooldownDuration)
    {
        if (wall == null)
        {
            if (showDebugLogs)
                //Debug.LogWarning("WallComboIncrementWithCooldown called with null wall!");
            return;
        }
        
        // Use absolute value to handle negative speeds properly
        float absRelativeVelocity = Mathf.Abs(relativeVelocity);
        
        if (absRelativeVelocity <= 0f)
        {
            if (showDebugLogs)
                //Debug.LogWarning($"Zero or negative relative velocity for wall: {relativeVelocity}");
            return;
        }
        
        // Check if this wall is on cooldown
        if (IsWallOnCooldown(wall))
        {
            if (showDebugLogs)
                //Debug.Log($"Wall {wall.name} is on cooldown, no combo added");
            return;
        }
        
        // Calculate combo as absolute relative velocity × wallVelocityMultiplier
        float calculatedCombo = absRelativeVelocity * wallVelocityMultiplier;
        
        float oldCombo = currentCombo;
        
        // Add base combo from wall bounce
        AddCombo(calculatedCombo);
        
        // Multiply current combo by wallComboMultiplier
        if (wallComboMultiplier != 1.0f)
        {
            currentCombo *= wallComboMultiplier;
            currentCombo = Mathf.Min(maxCombo, currentCombo);
        }
        
        // Custom cooldown logic: Set current wall to max cooldown and reset all others
        SetWallCooldownWithDuration(wall, customCooldownDuration);
        ResetAllOtherWallCooldowns(wall);
        
        if (showDebugLogs)
        {
            //Debug.Log($"Wall combo with custom cooldown: {oldCombo:F1} → {currentCombo:F1} (Added: {calculatedCombo:F1}, Velocity: {absRelativeVelocity:F2})");
        }
    }

    // New alternating wall combo system: only one wall type can give combo at a time
    public void WallComboIncrementAlternating(GameObject wall, float relativeVelocity)
    {
        if (wall == null)
        {
            if (showDebugLogs)
                //Debug.LogWarning("WallComboIncrementAlternating called with null wall!");
            return;
        }
        
        // Use absolute value to handle negative speeds properly
        float absRelativeVelocity = Mathf.Abs(relativeVelocity);
        
        if (absRelativeVelocity <= 0f)
        {
            if (showDebugLogs)
                //Debug.LogWarning($"Zero or negative relative velocity for wall: {relativeVelocity}");
            return;
        }
        
        // Get the wall side from the SideWall component
        SideWall sideWall = wall.GetComponent<SideWall>();
        if (sideWall == null)
        {
            if (showDebugLogs)
                //Debug.LogWarning($"Wall {wall.name} doesn't have SideWall component!");
            return;
        }
        
        // Check if this wall type is allowed to give combo based on current state
        if (!CanWallGiveCombo(sideWall.wallSide))
        {
            if (showDebugLogs)
                //Debug.Log($"Wall {sideWall.wallSide} cannot give combo in current state: {currentWallComboState}");
            return;
        }
        
        // Calculate combo as absolute relative velocity × wallVelocityMultiplier
        float calculatedCombo = absRelativeVelocity * wallVelocityMultiplier;
        
        float oldCombo = currentCombo;
        
        // Add base combo from wall bounce
        AddCombo(calculatedCombo);
        
        // Multiply current combo by wallComboMultiplier
        if (wallComboMultiplier != 1.0f)
        {
            currentCombo *= wallComboMultiplier;
            currentCombo = Mathf.Min(maxCombo, currentCombo);
        }
        
        // Update the wall combo state based on which wall was hit
        UpdateWallComboState(sideWall.wallSide);
        
        if (showDebugLogs)
        {
            //Debug.Log($"Alternating wall combo: {oldCombo:F1} → {currentCombo:F1} (Added: {calculatedCombo:F1}, Velocity: {absRelativeVelocity:F2}, New State: {currentWallComboState})");
        }
    }

    // New individual wall cooldown system: each wall has its own cooldown
    public void WallComboIncrementWithIndividualCooldown(GameObject wall, float relativeVelocity, float customDuration = -1f)
    {
        if (wall == null)
        {
            if (showDebugLogs)
                //Debug.LogWarning("WallComboIncrementWithIndividualCooldown called with null wall!");
            return;
        }
        
        // Use absolute value to handle negative speeds properly
        float absRelativeVelocity = Mathf.Abs(relativeVelocity);
        
        if (absRelativeVelocity <= 0f)
        {
            if (showDebugLogs)
                //Debug.LogWarning($"Zero or negative relative velocity for wall: {relativeVelocity}");
            return;
        }
        
        // Get the wall side from the SideWall component
        SideWall sideWall = wall.GetComponent<SideWall>();
        if (sideWall == null)
        {
            if (showDebugLogs)
                //Debug.LogWarning($"Wall {wall.name} doesn't have SideWall component!");
            return;
        }
        
        // Check if this wall is on cooldown
        bool wasOnCooldown = IsWallOnIndividualCooldown(sideWall.wallSide);
        
        if (wasOnCooldown)
        {
            // Wall is on cooldown - reset its cooldown to full duration without giving combo
            SetWallIndividualCooldown(sideWall.wallSide);
            ResetOppositeWallCooldown(sideWall.wallSide);
            
            if (showDebugLogs)
                //Debug.Log($"Wall {sideWall.wallSide} was on cooldown - cooldown reset to full duration, no combo added");
            return;
        }
        
        // Calculate combo as absolute relative velocity × wallVelocityMultiplier
        float calculatedCombo = absRelativeVelocity * wallVelocityMultiplier;
        
        float oldCombo = currentCombo;
        
        // Add base combo from wall bounce
        AddCombo(calculatedCombo);
        
        // Multiply current combo by wallComboMultiplier
        if (wallComboMultiplier != 1.0f)
        {
            currentCombo *= wallComboMultiplier;
            currentCombo = Mathf.Min(maxCombo, currentCombo);
        }
        
        // Set cooldown for this wall and reset the opposite wall's cooldown
        if (customDuration > 0f)
        {
            SetWallIndividualCooldownWithDuration(sideWall.wallSide, customDuration);
        }
        else
        {
            SetWallIndividualCooldown(sideWall.wallSide);
        }
        ResetOppositeWallCooldown(sideWall.wallSide);
        
        if (showDebugLogs)
        {
            //Debug.Log($"Individual wall combo: {oldCombo:F1} → {currentCombo:F1} (Added: {calculatedCombo:F1}, Velocity: {absRelativeVelocity:F2}, Left CD: {leftWallCooldown:F1}, Right CD: {rightWallCooldown:F1})");
        }
    }
    
    private void AddCombo(float amount)
    {
        if (amount < 0f)
        {
            if (showDebugLogs)
                //Debug.LogWarning($"Trying to add negative combo: {amount}");
            return;
        }
        
        currentCombo = Mathf.Min(maxCombo, currentCombo + amount);
    }

    private bool IsWallOnCooldown(GameObject wall)
    {
        return wallCooldowns.ContainsKey(wall) && Time.time < wallCooldowns[wall];
    }
    
    private void SetWallCooldown(GameObject wall)
    {
        wallCooldowns[wall] = Time.time + wallCooldownDuration;
    }

    private void SetWallCooldownWithDuration(GameObject wall, float duration)
    {
        wallCooldowns[wall] = Time.time + duration;
    }
    
    private void ResetOtherWallCooldowns(GameObject currentWall)
    {
        expiredCooldowns.Clear(); // Reuse the list
        
        foreach (var kvp in wallCooldowns)
        {
            if (kvp.Key != currentWall && kvp.Key != null) // Added null check
            {
                expiredCooldowns.Add(kvp.Key);
            }
        }
        
        foreach (var wall in expiredCooldowns)
        {
            wallCooldowns.Remove(wall);
        }
    }

    private void ResetAllOtherWallCooldowns(GameObject currentWall)
    {
        expiredCooldowns.Clear(); // Reuse the list
        
        foreach (var kvp in wallCooldowns)
        {
            if (kvp.Key != currentWall && kvp.Key != null) // Added null check
            {
                expiredCooldowns.Add(kvp.Key);
            }
        }
        
        // Remove all other walls from cooldown (reset them)
        foreach (var wall in expiredCooldowns)
        {
            wallCooldowns.Remove(wall);
        }
    }
    
    private void UpdateWallCooldowns()
    {
        if (wallCooldowns.Count == 0) return; // Early exit if no cooldowns
        
        expiredCooldowns.Clear(); // Reuse the list
        
        float currentTime = Time.time; // Cache current time
        foreach (var kvp in wallCooldowns)
        {
            if (kvp.Key == null || currentTime >= kvp.Value) // Added null check
            {
                expiredCooldowns.Add(kvp.Key);
            }
        }
        
        // Remove expired cooldowns
        for (int i = 0; i < expiredCooldowns.Count; i++)
        {
            wallCooldowns.Remove(expiredCooldowns[i]);
        }
    }
    
    public float GetComboPercentage()
    {
        if (maxCombo <= 0f) return 0f; // Prevent division by zero
        return (currentCombo / maxCombo) * 100f;
    }
    
    public void ResetCombo()
    {
        if (showDebugLogs){
             //Debug.Log("Combo reset to 0");
        }

            
        currentCombo = 0f;
        wallCooldowns.Clear();
        
        // Reset individual wall cooldowns
        if (useWallCooldownSystem)
        {
            leftWallCooldown = 0f;
            rightWallCooldown = 0f;
        }
        
        // Reset alternating wall combo state
        if (useAlternatingWallCombo)
        {
            currentWallComboState = WallComboState.BothActive;
        }
    }
    
    // Additional utility methods
    public bool IsComboAtMax()
    {
        return currentCombo >= maxCombo;
    }
    
    public void SetCombo(float value)
    {
        currentCombo = Mathf.Clamp(value, 0f, maxCombo);
        
        if (showDebugLogs){
            //Debug.Log($"Combo set to: {currentCombo}");
        }
        
    }
    
    // Method to test combo functionality
    [ContextMenu("Test Combo Increment")]
    private void TestComboIncrement()
    {
        AddCombo(100f);
        //Debug.Log($"Test: Combo is now {currentCombo}");
    }
    
    // Helper methods for alternating wall combo system
    private bool CanWallGiveCombo(SideWall.WallSide wallSide)
    {
        if (!useAlternatingWallCombo)
            return true; // If alternating system is disabled, all walls can give combo
            
        switch (currentWallComboState)
        {
            case WallComboState.BothActive:
                return true; // Both walls can give combo initially
            case WallComboState.LeftOnly:
                return wallSide == SideWall.WallSide.Left; // Only left wall can give combo
            case WallComboState.RightOnly:
                return wallSide == SideWall.WallSide.Right; // Only right wall can give combo
            default:
                return true;
        }
    }
    
    private void UpdateWallComboState(SideWall.WallSide hitWallSide)
    {
        if (!useAlternatingWallCombo)
            return; // Don't update state if alternating system is disabled
            
        switch (currentWallComboState)
        {
            case WallComboState.BothActive:
                // After first hit, switch to only the opposite wall being active
                if (hitWallSide == SideWall.WallSide.Left)
                    currentWallComboState = WallComboState.RightOnly;
                else if (hitWallSide == SideWall.WallSide.Right)
                    currentWallComboState = WallComboState.LeftOnly;
                break;
                
            case WallComboState.LeftOnly:
                // If left wall was hit, now only right wall can give combo
                if (hitWallSide == SideWall.WallSide.Left)
                    currentWallComboState = WallComboState.RightOnly;
                break;
                
            case WallComboState.RightOnly:
                // If right wall was hit, now only left wall can give combo
                if (hitWallSide == SideWall.WallSide.Right)
                    currentWallComboState = WallComboState.LeftOnly;
                break;
        }
    }
    
    // Public method to get current wall combo state (for debugging)
    public WallComboState GetCurrentWallComboState()
    {
        return currentWallComboState;
    }
    
    // Public method to manually set wall combo state (for testing)
    public void SetWallComboState(WallComboState newState)
    {
        currentWallComboState = newState;
    }
    
    // Helper methods for individual wall cooldown system
    private bool IsWallOnIndividualCooldown(SideWall.WallSide wallSide)
    {
        if (!useWallCooldownSystem)
            return false; // If cooldown system is disabled, walls are never on cooldown
            
        switch (wallSide)
        {
            case SideWall.WallSide.Left:
                return leftWallCooldown > Time.time;
            case SideWall.WallSide.Right:
                return rightWallCooldown > Time.time;
            default:
                return false;
        }
    }
    
    public void SetWallIndividualCooldown(SideWall.WallSide wallSide)
    {
        if (!useWallCooldownSystem)
            return;
            
        switch (wallSide)
        {
            case SideWall.WallSide.Left:
                leftWallCooldown = Time.time + wallCooldownDuration;
                break;
            case SideWall.WallSide.Right:
                rightWallCooldown = Time.time + wallCooldownDuration;
                break;
        }
    }
    
    public void SetWallIndividualCooldownWithDuration(SideWall.WallSide wallSide, float duration)
    {
        if (!useWallCooldownSystem)
            return;
            
        switch (wallSide)
        {
            case SideWall.WallSide.Left:
                leftWallCooldown = Time.time + duration;
                break;
            case SideWall.WallSide.Right:
                rightWallCooldown = Time.time + duration;
                break;
        }
    }
    
    public void ResetOppositeWallCooldown(SideWall.WallSide hitWallSide)
    {
        if (!useWallCooldownSystem)
            return;
            
        switch (hitWallSide)
        {
            case SideWall.WallSide.Left:
                // Hit left wall, reset right wall cooldown
                rightWallCooldown = 0f;
                break;
            case SideWall.WallSide.Right:
                // Hit right wall, reset left wall cooldown
                leftWallCooldown = 0f;
                break;
        }
    }
    
    // Public methods to get cooldown status (for debugging)
    public bool IsLeftWallOnCooldown()
    {
        return IsWallOnIndividualCooldown(SideWall.WallSide.Left);
    }
    
    public bool IsRightWallOnCooldown()
    {
        return IsWallOnIndividualCooldown(SideWall.WallSide.Right);
    }
    
    public float GetLeftWallCooldownRemaining()
    {
        return Mathf.Max(0f, leftWallCooldown - Time.time);
    }
    
    public float GetRightWallCooldownRemaining()
    {
        return Mathf.Max(0f, rightWallCooldown - Time.time);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}