using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentLoader : MonoBehaviour
{
    [Header("Game Persistent Scene")]
    public string gamePersistentScene = "GamePersistentScene";
    
    [Header("Auto-load Settings")]
    public bool autoLoad = true;
    
    private static bool gameManagersLoaded = false;

    void Awake()
    {
        if (autoLoad)
        {
            LoadRequiredScenes();
        }
    }

    void Start()
    {
        // Ensure scenes are loaded even if Awake didn't run
        if (autoLoad)
        {
            LoadRequiredScenes();
        }
    }

    public void LoadRequiredScenes()
    {
        // Since this script only exists in game scenes, always load game persistent scene
        LoadGamePersistentScene();
    }





    void LoadGamePersistentScene()
    {
        if (!gameManagersLoaded && !string.IsNullOrEmpty(gamePersistentScene))
        {
            Scene gameScene = SceneManager.GetSceneByName(gamePersistentScene);
            if (!gameScene.isLoaded)
            {
                Debug.Log($"[PersistentLoader] Loading game persistent scene: {gamePersistentScene}");
                SceneManager.LoadSceneAsync(gamePersistentScene, LoadSceneMode.Additive);
                gameManagersLoaded = true;
            }
            else
            {
                Debug.Log($"[PersistentLoader] Game persistent scene {gamePersistentScene} already loaded");
                gameManagersLoaded = true;
            }
        }
    }

    void UnloadGamePersistentScene()
    {
        if (gameManagersLoaded && !string.IsNullOrEmpty(gamePersistentScene))
        {
            Scene gameScene = SceneManager.GetSceneByName(gamePersistentScene);
            if (gameScene.isLoaded)
            {
                Debug.Log($"[PersistentLoader] Unloading game persistent scene: {gamePersistentScene}");
                SceneManager.UnloadSceneAsync(gamePersistentScene);
                gameManagersLoaded = false;
            }
        }
    }

    // Reset flags when application starts (for editor testing)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticData()
    {
        gameManagersLoaded = false;
    }

    // Public method to check status
    public static bool AreGameManagersLoaded()
    {
        return gameManagersLoaded;
    }
    
    // Public method to reset for scene restart
    public static void ResetForRestart()
    {
        gameManagersLoaded = false;
        Debug.Log("[PersistentLoader] Reset for scene restart - GamePersistentScene will be reloaded");
    }

    // Manual control methods
    [ContextMenu("Force Load Game Persistent Scene")]
    public void ForceLoadGamePersistentScene()
    {
        gameManagersLoaded = false;
        LoadRequiredScenes();
    }

    [ContextMenu("Force Unload Game Managers")]
    public void ForceUnloadGameManagers()
    {
        UnloadGamePersistentScene();
    }

    [ContextMenu("Debug Scene Info")]
    public void DebugSceneInfo()
    {
        Debug.Log($"=== PERSISTENT LOADER DEBUG ===");
        Debug.Log($"Current Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"Game Managers Loaded: {gameManagersLoaded}");
        Debug.Log($"Loaded Scenes: {SceneManager.sceneCount}");
        
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            Debug.Log($"  Scene {i}: {scene.name} (loaded: {scene.isLoaded})");
        }
        Debug.Log($"==============================");
    }
}
