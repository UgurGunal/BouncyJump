using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public enum CameraMode { Smooth, Instant }
    public CameraMode cameraMode = CameraMode.Smooth;
    public Transform player;
    public float smoothSpeed = 5f;
    public float constantSpeed = 1f; // Constant upward speed
    public float yOffset = 2f;
    public float movementThreshold = 1f; // Distance player must move before camera starts following

    private bool cameraActivated = false;
    private float highestCameraY = 0f;
    private Camera mainCamera;

    private bool hasTriggeredRestart = false; // Add this line

    [Header("Game Over Settings")]
    public float restartMargin = 0f;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Check if player is out of bounds
        CheckCameraBounds();

        // Check if camera should be activated (one-time trigger)
        if (!cameraActivated && player.position.y > transform.position.y + movementThreshold)
        {
            cameraActivated = true;
            highestCameraY = transform.position.y;
        }

        if (cameraActivated)
        {
            // Move camera up at a constant speed
            transform.position += new Vector3(0, constantSpeed * Time.deltaTime, 0);

            Vector3 targetPos = new Vector3(transform.position.x, player.position.y + yOffset, transform.position.z);

            // Only follow upward, never go down
            if (targetPos.y > highestCameraY)
            {
                if (cameraMode == CameraMode.Smooth)
                {
                    // Smooth follow upward only
                    transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
                }
                else if (cameraMode == CameraMode.Instant)
                {
                    // Instant follow upward only
                    transform.position = targetPos;
                }

                // Update highest camera Y position
                highestCameraY = Mathf.Max(highestCameraY, transform.position.y);
            }
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
}