using UnityEngine;

/// <summary>
/// Architecture Guide for LevelManager in Tower Game
/// 
/// RECOMMENDED APPROACH: Scene-Specific LevelManager
/// ==================================================
/// 
/// Each tower scene has its own LevelManager instance with:
/// - Tower-specific platform prefabs
/// - Tower-specific spawn rates
/// - Tower-specific camera settings
/// - Tower-specific visual themes
/// 
/// SCENE STRUCTURE:
/// ================
/// 
/// GamePersistentScene:
/// ├── PointsManager (global progress tracking)
/// ├── TowerManager (tower selection/currency)
/// ├── Player + Camera (common game objects)
/// └── Game UI (common interface)
/// 
/// BasicTowerScene:
/// ├── LevelManager (Basic tower configuration)
/// │   ├── Basic platform prefabs
/// │   ├── Basic spawn rates
/// │   └── Basic visual settings
/// └── Basic-themed level objects
/// 
/// RoyalTowerScene:
/// ├── LevelManager (Royal tower configuration)
/// │   ├── Royal platform prefabs
/// │   ├── Royal spawn rates (maybe harder?)
/// │   └── Royal visual settings
/// └── Royal-themed level objects
/// 
/// WHY THIS WORKS WELL:
/// ====================
/// 
/// 1. Clear Separation:
///    - Global managers in GamePersistentScene
///    - Tower-specific managers in tower scenes
/// 
/// 2. Designer Workflow:
///    - Open BasicTowerScene → Configure basic settings
///    - Open RoyalTowerScene → Configure royal settings
///    - No need to manage separate asset files
/// 
/// 3. Runtime Efficiency:
///    - Only one LevelManager active at a time
///    - No config switching overhead
///    - Direct access to tower-specific data
/// 
/// 4. Easy Debugging:
///    - All tower settings visible in scene
///    - No external asset dependencies
///    - Clear what belongs to which tower
/// 
/// ALTERNATIVE APPROACHES (NOT RECOMMENDED):
/// =========================================
/// 
/// ScriptableObject Approach:
/// ❌ TowerConfigSO for each tower
/// ❌ LevelManager.LoadConfig(towerConfigSO) at runtime
/// ❌ More complex, unnecessary for this use case
/// 
/// Single Global LevelManager:
/// ❌ Huge config arrays for all towers
/// ❌ Complex switching logic
/// ❌ Hard to maintain and debug
/// 
/// IMPLEMENTATION DETAILS:
/// =======================
/// 
/// LevelManager should:
/// ✅ Be scene-specific (not in GamePersistentScene)
/// ✅ Have singleton pattern for easy access
/// ✅ Configure tower-specific settings
/// ✅ Reference tower-specific prefabs
/// ✅ Handle level progression within that tower
/// 
/// Example LevelManager setup per tower:
/// 
/// BasicTowerScene/LevelManager:
/// - levels[0]: Basic platforms, easy spawn rates
/// - levels[1]: Basic platforms, medium spawn rates
/// - levels[2]: Basic platforms, hard spawn rates
/// 
/// RoyalTowerScene/LevelManager:
/// - levels[0]: Royal platforms, easy spawn rates
/// - levels[1]: Royal platforms, medium spawn rates  
/// - levels[2]: Royal platforms, hard spawn rates
/// 
/// </summary>
public class LevelManagerArchitecture : MonoBehaviour
{
    [Header("Architecture Validation")]
    [Tooltip("Check this to validate the current scene's architecture")]
    public bool validateArchitecture = false;
    
    void Start()
    {
        if (validateArchitecture)
        {
            ValidateCurrentArchitecture();
        }
    }
    
    [ContextMenu("Validate Scene Architecture")]
    public void ValidateCurrentArchitecture()
    {
        Debug.Log("=== LEVEL MANAGER ARCHITECTURE VALIDATION ===");
        
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"Current Scene: {sceneName}");
        
        // Check for LevelManager in current scene
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            Debug.Log("✅ LevelManager found in current scene");
            Debug.Log($"   - Level Count: {levelManager.levelCount}");
            Debug.Log($"   - Level Height: {levelManager.levelHeight}");
            
            // Check if it has tower-specific data
            if (levelManager.levels != null && levelManager.levels.Length > 0)
            {
                Debug.Log($"✅ Level data configured ({levelManager.levels.Length} levels)");
                
                // Check first level for platform prefabs
                var firstLevel = levelManager.levels[0];
                bool hasPlatforms = firstLevel.longPlatformPrefab != null || 
                                   firstLevel.shortPlatformPrefab != null || 
                                   firstLevel.specialPlatformPrefab != null;
                
                if (hasPlatforms)
                {
                    Debug.Log("✅ Platform prefabs configured");
                }
                else
                {
                    Debug.LogWarning("⚠️ Platform prefabs not configured");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Level data not configured");
            }
        }
        else
        {
            if (sceneName.ToLower().Contains("home") || sceneName.ToLower().Contains("menu"))
            {
                Debug.Log("✅ No LevelManager needed in menu scene");
            }
            else
            {
                Debug.LogWarning("⚠️ LevelManager not found in game scene");
            }
        }
        
        // Check for persistent managers
        bool pointsManagerPersistent = PointsManager.Instance != null;
        bool towerManagerPersistent = TowerManager.Instance != null;
        
        Debug.Log($"PointsManager (persistent): {(pointsManagerPersistent ? "✅ Found" : "❌ Missing")}");
        Debug.Log($"TowerManager (persistent): {(towerManagerPersistent ? "✅ Found" : "❌ Missing")}");
        
        Debug.Log("============================================");
    }
    
    [ContextMenu("Show Architecture Recommendations")]
    public void ShowArchitectureRecommendations()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        Debug.Log("=== ARCHITECTURE RECOMMENDATIONS ===");
        Debug.Log($"For scene: {sceneName}");
        
        if (sceneName.ToLower().Contains("home") || sceneName.ToLower().Contains("menu"))
        {
            Debug.Log("MENU SCENE - Should have:");
            Debug.Log("✅ TowerShopManager (scene-specific)");
            Debug.Log("✅ HomeScreenUI (scene-specific)");
            Debug.Log("✅ PersistentLoader (loads GamePersistentScene for gameplay)");
            Debug.Log("❌ No LevelManager needed");
        }
        else
        {
            Debug.Log("TOWER SCENE - Should have:");
            Debug.Log("✅ LevelManager (scene-specific, tower-themed)");
            Debug.Log("✅ Tower-specific level objects");
            Debug.Log("✅ PersistentLoader (loads GamePersistentScene)");
            Debug.Log("❌ No duplicate managers from GamePersistentScene");
        }
        
        Debug.Log("===================================");
    }
}
