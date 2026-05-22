using UnityEngine;
using System;

/// <summary>
/// Alternative approach for managing cross-scene references
/// This provides a more robust solution for tower-specific managers that need persistent scene references
/// </summary>
public class CrossSceneReferenceManager : MonoBehaviour
{
    public static CrossSceneReferenceManager Instance { get; private set; }
    
    [Header("Persistent Scene References")]
    public Transform player;
    public CameraFollow cameraFollow;
    public Camera mainCamera;
    
    // Events for when references are ready
    public static event Action<Transform> OnPlayerReady;
    public static event Action<CameraFollow> OnCameraFollowReady;
    public static event Action<Camera> OnMainCameraReady;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Auto-find references if not assigned
        FindAllReferences();
        
        // Notify subscribers that references are ready
        NotifyReferencesReady();
    }
    
    void FindAllReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
        
        if (cameraFollow == null)
        {
            cameraFollow = FindObjectOfType<CameraFollow>();
            if (cameraFollow != null)
            {
            }
        }
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
            }
        }
    }
    
    void NotifyReferencesReady()
    {
        if (player != null)
            OnPlayerReady?.Invoke(player);
            
        if (cameraFollow != null)
            OnCameraFollowReady?.Invoke(cameraFollow);
            
        if (mainCamera != null)
            OnMainCameraReady?.Invoke(mainCamera);
    }
    
    // Public methods for getting references
    public Transform GetPlayer()
    {
        if (player == null)
            FindAllReferences();
        return player;
    }
    
    public CameraFollow GetCameraFollow()
    {
        if (cameraFollow == null)
            FindAllReferences();
        return cameraFollow;
    }
    
    public Camera GetMainCamera()
    {
        if (mainCamera == null)
            FindAllReferences();
        return mainCamera;
    }
    
    // Reset when application starts (for editor testing)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticData()
    {
        OnPlayerReady = null;
        OnCameraFollowReady = null;
        OnMainCameraReady = null;
    }
}
