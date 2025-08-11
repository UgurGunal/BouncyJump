using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public enum CameraMode { Smooth, Instant }
    public CameraMode cameraMode = CameraMode.Smooth;
    public Transform player;
    public float smoothSpeed = 5f;
    [SerializeField] private float constantSpeed = 1f; // Constant upward speed - now private with public property
    public float yOffset = -1f;
    public float movementThreshold = 10f; // Distance player must move before camera starts following

    private bool cameraActivated = false;
    private float highestCameraY = 0f;
    private Camera mainCamera;
    private bool hasTriggeredRestart = false;
    private bool constantSpeedActive = false; // New flag to track constant speed activation

    [Header("Game Over Settings")]
    public float restartMargin = 0f;

    // Public property to access and modify constantSpeed
    public float ConstantSpeed
    {
        get { return constantSpeed; }
        set { constantSpeed = Mathf.Max(0f, value); } // Ensure speed is never negative
    }

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        
        // Initialize camera speed from LevelManager if available
        if (LevelManager.Instance != null)
        {
            int currentLevel = LevelManager.Instance.GetCurrentLevel(0f);
            LevelManager.LevelData levelData = LevelManager.Instance.GetLevelData(currentLevel);
            constantSpeed = levelData.cameraSpeed;
            // Debug.Log($"Initial camera speed set to: {constantSpeed}");
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Check if player is out of bounds
        CheckCameraBounds();

        // Debug: Show current positions and threshold check
        if (!cameraActivated)
        {
            // Debug.Log($"Player Y: {player.position.y}, Threshold: {movementThreshold}, Should activate: {player.position.y > movementThreshold}");
        }

        // Check if camera should be activated (one-time trigger for upward speed)
        if (!cameraActivated && player.position.y > movementThreshold)
        {
            cameraActivated = true;
            constantSpeedActive = true; // Mark constant speed as active
            highestCameraY = transform.position.y;
            
            // Fetch current level's camera speed when threshold is passed
            if (LevelManager.Instance != null)
            {
                int currentLevel = LevelManager.Instance.GetCurrentLevel(player.position.y);
                LevelManager.LevelData levelData = LevelManager.Instance.GetLevelData(currentLevel);
                constantSpeed = levelData.cameraSpeed;
                // Debug.Log($"Camera upward speed ACTIVATED - Level {currentLevel}, Speed: {constantSpeed}");
            }
            else
            {
                // Debug.Log("Camera upward speed ACTIVATED - LevelManager not found, using default speed");
            }
        }

        // Apply constant upward speed FIRST (always when active, regardless of player position)
        if (constantSpeedActive)
        {
            // Move camera up at constant speed
            Vector3 constantMovement = new Vector3(0, constantSpeed * Time.deltaTime, 0);
            transform.position += constantMovement;
            
            // Update highest camera Y position after constant speed
            highestCameraY = Mathf.Max(highestCameraY, transform.position.y);
        }

        // Camera follows player smoothly ONLY when they're above the offset
        Vector3 targetPos = new Vector3(transform.position.x, player.position.y + yOffset, transform.position.z);

        // Follow player upward smoothly when they pass the yOffset
        if (player.position.y > transform.position.y + yOffset)
        {
            if (cameraMode == CameraMode.Smooth)
            {
                // Smooth follow upward using Lerp
                transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
            }
            else if (cameraMode == CameraMode.Instant)
            {
                // Instant follow upward
                transform.position = targetPos;
            }

            // Update highest camera Y position
            highestCameraY = Mathf.Max(highestCameraY, transform.position.y);
        }

        // Ensure camera never goes below its highest position (prevent downward movement)
        if (transform.position.y < highestCameraY)
        {
            transform.position = new Vector3(transform.position.x, highestCameraY, transform.position.z);
        }
    }

    void CheckCameraBounds()
    {
        if (hasTriggeredRestart) return; // Guard to prevent multiple calls
        
        if (mainCamera != null)
        {
            Vector3 viewportPoint = mainCamera.WorldToViewportPoint(player.position);
            if (viewportPoint.y < -restartMargin)
            {
                hasTriggeredRestart = true; // Set guard
                RestartGame();
            }
        }
    }

    void RestartGame()
    {
        // End session first to capture final stats before pausing
        if (PointsManager.Instance != null)
        {
            PointsManager.Instance.EndSession();
        }

        // Pause the game immediately
        Time.timeScale = 0f; 

        // Then, show the revive panel
        if (RevivePanelUI.Instance != null && player != null)
        {
            RevivePanelUI.Instance.ShowRevivePanel();
        }
        else
        {
            // Fallback if RevivePanelUI is not in scene or player is null
            // Resume time before loading new scene in fallback
            Time.timeScale = 1f; 
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    // Reset the restart trigger to allow player to die again after revive
    public void ResetRestartTrigger()
    {
        hasTriggeredRestart = false;
    }

    // Method to update camera speed from external scripts (like TowerGenerator)
    public void UpdateCameraSpeed(float newSpeed)
    {
        ConstantSpeed = newSpeed;
        // Debug.Log($"Camera speed updated to: {newSpeed}");
    }
}