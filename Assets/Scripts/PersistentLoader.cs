using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentLoader : MonoBehaviour
{
    [Header("Game Persistent Scene")]
    public string gamePersistentScene = "GamePersistentScene";
    
    [Header("Auto-load Settings")]
    public bool autoLoad = true;
    
    private static string persistentSceneName;
    private static bool loadInProgress;

    void Awake()
    {
        persistentSceneName = gamePersistentScene;
        if (autoLoad)
            LoadRequiredScenes();
    }

    void OnEnable()
    {
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    void OnDisable()
    {
        SceneManager.sceneUnloaded -= HandleSceneUnloaded;
    }

    void Start()
    {
        if (autoLoad)
            LoadRequiredScenes();
    }

    void HandleSceneUnloaded(Scene scene)
    {
        if (!string.IsNullOrEmpty(persistentSceneName) && scene.name == persistentSceneName)
        {
            loadInProgress = false;
            Debug.Log($"[PersistentLoader] Persistent scene unloaded: {persistentSceneName}");
        }
    }

    public void LoadRequiredScenes()
    {
        LoadGamePersistentScene();
    }

    void LoadGamePersistentScene()
    {
        if (string.IsNullOrEmpty(gamePersistentScene))
            return;

        persistentSceneName = gamePersistentScene;
        Scene gameScene = SceneManager.GetSceneByName(gamePersistentScene);
        if (gameScene.isLoaded)
            return;

        if (loadInProgress)
            return;

        loadInProgress = true;
        Debug.Log($"[PersistentLoader] Loading game persistent scene: {gamePersistentScene}");
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(gamePersistentScene, LoadSceneMode.Additive);
        loadOp.completed += _ =>
        {
            loadInProgress = false;
            Debug.Log($"[PersistentLoader] Game persistent scene loaded: {gamePersistentScene}");
        };
    }

    void UnloadGamePersistentScene()
    {
        if (string.IsNullOrEmpty(gamePersistentScene))
            return;

        Scene gameScene = SceneManager.GetSceneByName(gamePersistentScene);
        if (gameScene.isLoaded)
        {
            Debug.Log($"[PersistentLoader] Unloading game persistent scene: {gamePersistentScene}");
            SceneManager.UnloadSceneAsync(gamePersistentScene);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticData()
    {
        persistentSceneName = null;
        loadInProgress = false;
    }

    public static bool AreGameManagersLoaded()
    {
        if (string.IsNullOrEmpty(persistentSceneName))
            return false;

        Scene gameScene = SceneManager.GetSceneByName(persistentSceneName);
        return gameScene.isLoaded;
    }
    
    public static void ResetForRestart()
    {
        loadInProgress = false;
        Debug.Log("[PersistentLoader] Reset for restart — persistent scene will load again on next tower scene");
    }

    // Manual control methods
    [ContextMenu("Force Load Game Persistent Scene")]
    public void ForceLoadGamePersistentScene()
    {
        loadInProgress = false;
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
        Debug.Log($"Game Managers Loaded: {AreGameManagersLoaded()}");
        Debug.Log($"Loaded Scenes: {SceneManager.sceneCount}");
        
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            Debug.Log($"  Scene {i}: {scene.name} (loaded: {scene.isLoaded})");
        }
        Debug.Log($"==============================");
    }
}
