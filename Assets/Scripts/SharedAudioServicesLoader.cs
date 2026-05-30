using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures SoundEffectsManager and MusicManager exist when starting from HomeScene.
/// Instantiates a lightweight SharedAudioServices prefab instead of loading PersistentScene.
/// </summary>
public class SharedAudioServicesLoader : MonoBehaviour
{
    const string HomeSceneName = "HomeScene";
    const string DefaultResourcesPrefabPath = "SharedAudioServices";

    [Tooltip("Optional override. If empty, loads Resources/SharedAudioServices.prefab.")]
    [SerializeField] GameObject audioServicesPrefab;

    static GameObject cachedPrefab;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootstrapForHomeScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || activeScene.name != HomeSceneName)
            return;

        if (SoundEffectsManager.Instance != null)
            return;

        if (Object.FindObjectOfType<SharedAudioServicesLoader>() != null)
            return;

        var loaderObject = new GameObject(nameof(SharedAudioServicesLoader));
        loaderObject.AddComponent<SharedAudioServicesLoader>();
    }

    void Awake()
    {
        if (SoundEffectsManager.Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        GameObject prefab = ResolvePrefab();
        if (prefab == null)
        {
            Debug.LogWarning(
                "SharedAudioServicesLoader: Could not find SharedAudioServices prefab. " +
                "Create Assets/Resources/SharedAudioServices.prefab from PersistentScene > AudioManager.");
            Destroy(gameObject);
            return;
        }

        Instantiate(prefab);
        Destroy(gameObject);
    }

    GameObject ResolvePrefab()
    {
        if (audioServicesPrefab != null)
            return audioServicesPrefab;

        if (cachedPrefab != null)
            return cachedPrefab;

        cachedPrefab = Resources.Load<GameObject>(DefaultResourcesPrefabPath);
        return cachedPrefab;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticState()
    {
        cachedPrefab = null;
    }
}
