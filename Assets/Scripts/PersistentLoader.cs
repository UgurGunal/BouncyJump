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
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(gamePersistentScene, LoadSceneMode.Additive);
        loadOp.completed += _ =>
        {
            loadInProgress = false;
        };
    }

    void UnloadGamePersistentScene()
    {
        if (string.IsNullOrEmpty(gamePersistentScene))
            return;

        Scene gameScene = SceneManager.GetSceneByName(gamePersistentScene);
        if (gameScene.isLoaded)
        {
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

}
