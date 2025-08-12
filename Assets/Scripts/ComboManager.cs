using UnityEngine;
using System.Collections.Generic;

public class ComboManager : MonoBehaviour
{
    [Header("Combo Settings")]
    public float maxCombo = 1000f;
    [SerializeField] private float currentCombo = 0f; // Made private with SerializeField for inspector visibility
    public float minDecrease = 60f; // Minimum decay rate per second
    public float maxDecrease = 260f; // Maximum decay rate per second
    
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

    [Header("Wall Cooldown")]
    public float wallCooldownDuration = 1.5f;

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
        
        if (relativeVelocity < 0f)
        {
            if (showDebugLogs)
                //Debug.LogWarning($"Negative relative velocity for wall: {relativeVelocity}");
            return;
        }
        
        // Check if this wall is on cooldown
        if (IsWallOnCooldown(wall))
        {
            if (showDebugLogs)
                //Debug.Log($"Wall {wall.name} is on cooldown, no combo added");
            return;
        }
        
        // Calculate combo as relative velocity X × wallVelocityMultiplier
        float calculatedCombo = relativeVelocity * wallVelocityMultiplier;
        
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
            //Debug.Log($"Wall combo: {oldCombo:F1} → {currentCombo:F1} (Added: {calculatedCombo:F1}, Velocity: {relativeVelocity:F2})");
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
        
        if (relativeVelocity < 0f)
        {
            if (showDebugLogs)
                //Debug.LogWarning($"Negative relative velocity for wall: {relativeVelocity}");
            return;
        }
        
        // Check if this wall is on cooldown
        if (IsWallOnCooldown(wall))
        {
            if (showDebugLogs)
                //Debug.Log($"Wall {wall.name} is on cooldown, no combo added");
            return;
        }
        
        // Calculate combo as relative velocity X × wallVelocityMultiplier
        float calculatedCombo = relativeVelocity * wallVelocityMultiplier;
        
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
            //Debug.Log($"Wall combo with custom cooldown: {oldCombo:F1} → {currentCombo:F1} (Added: {calculatedCombo:F1}, Velocity: {relativeVelocity:F2})");
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
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}