using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This script provides utilities and documentation for setting up persistent managers with additive scene loading.
/// 
/// SCENE STRUCTURE:
/// 
/// 1. GamePersistentScene (Game-Only Persistent) - Contains:
///    - Common game objects (Player controller, Camera, UI canvases)
///    - Game managers (PointsManager, TowerManager, AudioManager, etc.)
///    - Objects needed in ALL tower scenes but NOT in main menu
/// 
/// 2. HomeScene (Main Menu) - Contains:
///    - HomeScreenUI, ShopManager, HomeScreenCurrencyDisplay
///    - All managers needed for main menu functionality
///    - PersistentLoader (auto-detects as MainMenu, no persistent scenes loaded)
/// 
/// 3. TowerScenes (Gameplay) - Contains:
///    - Tower-specific level objects, platforms, themes
///    - PersistentLoader (auto-detects as GameScene, loads GamePersistentScene)
/// 
/// SETUP INSTRUCTIONS:
/// 
/// 1. Create "GamePersistentScene":
///    - Create new scene named "GamePersistentScene"
///    - Add common game objects (Player prefab, Main Camera, Game UI Canvas)
///    - Add all game managers (PointsManager, TowerManager, AudioManager, etc.)
///    - Configure all tower data in TowerManager
///    - These objects will exist in ALL tower scenes but NOT in main menu
///    - Save scene
/// 
/// 2. Update HomeScene:
///    - Add PersistentLoader script to an empty GameObject
///    - Set gamePersistentScene = "GamePersistentScene"
///    - Leave sceneType = "Auto" (it will detect as MainMenu automatically)
///    - HomeScene will NOT load any persistent scenes
/// 
/// 3. Update ALL TowerScenes:
///    - Add PersistentLoader script to an empty GameObject
///    - Set gamePersistentScene = "GamePersistentScene"
///    - Leave sceneType = "Auto" (it will detect as GameScene automatically)
///    - Remove any duplicate Player, Camera, or manager objects from these scenes
/// 
/// 4. Build Settings:
///    - Add GamePersistentScene to Build Settings
///    - Add HomeScene and all TowerScenes to Build Settings
/// 
/// HOW IT WORKS:
/// 
/// - When HomeScene loads â†’ PersistentLoader detects "MainMenu", no persistent scenes loaded
/// - When TowerScene loads â†’ PersistentLoader detects "GameScene", loads GamePersistentScene
/// - When switching between tower scenes â†’ GamePersistentScene stays loaded, only tower-specific content changes
/// - When returning to HomeScene â†’ PersistentLoader unloads GamePersistentScene
/// - Game managers (currency, towers) and objects (player, camera) only exist during gameplay
/// - HomeScene is completely independent with its own managers
/// - ONE script handles everything automatically based on scene name detection
/// 
/// </summary>
public class SceneManagerSetup : MonoBehaviour
{
    [Header("Scene Management")]
    [Tooltip("Use this to test scene loading in the editor")]
    public bool testInEditor = false;
    
    [ContextMenu("Load Home Scene")]
    public void LoadHomeScene()
    {
        SceneManager.LoadScene("HomeScene");
    }

    [ContextMenu("Load Game Scene")]
    public void LoadGameScene()
    {
        SceneManager.LoadScene("GameScene");
    }
}
