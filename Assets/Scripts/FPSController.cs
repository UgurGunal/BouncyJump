using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FPSController : MonoBehaviour
{
    [Header("FPS Settings")]
    public int targetFPS = 60;
    public bool limitFPS = true;
    
    [Header("FPS Display")]
    public bool showFPS = true;
    public TextMeshProUGUI fpsText; // Assign a TextMeshPro component
    public float updateInterval = 0.5f; // How often to update FPS display
    
    private float deltaTime = 0.0f;
    private float fps = 0.0f;
    private float timeSinceLastUpdate = 0.0f;
    
    void Start()
    {
        // Set target frame rate
        if (limitFPS)
        {
            Application.targetFrameRate = targetFPS;
            QualitySettings.vSyncCount = 0; // Disable vsync for mobile
        }
        
        // If no text component assigned, try to find one
        if (fpsText == null)
        {
            fpsText = FindObjectOfType<TextMeshProUGUI>();
        }
        
        // Create a simple text display if none exists
        if (fpsText == null && showFPS)
        {
            CreateFPSText();
        }
    }
    
    void Update()
    {
        // Calculate FPS
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        fps = 1.0f / deltaTime;
        
        // Update FPS display
        if (showFPS && fpsText != null)
        {
            timeSinceLastUpdate += Time.unscaledDeltaTime;
            if (timeSinceLastUpdate >= updateInterval)
            {
                UpdateFPSText();
                timeSinceLastUpdate = 0f;
            }
        }
    }
    
    void UpdateFPSText()
    {
        if (fpsText != null)
        {
            fpsText.text = "FPS: " + fps.ToString("F1");
        }
    }
    
    void CreateFPSText()
    {
        // Create a simple UI text for FPS display
        Canvas canvas = FindObjectOfType<Canvas>();
        GameObject canvasObj = null;
        
        if (canvas == null)
        {
            // Create canvas if none exists
            canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasObj = canvas.gameObject;
        }
        
        // Create text object
        GameObject textObj = new GameObject("FPSText");
        textObj.transform.SetParent(canvasObj.transform, false);
        
        fpsText = textObj.AddComponent<TextMeshProUGUI>();
        fpsText.fontSize = 24;
        fpsText.color = Color.white;
        fpsText.alignment = TextAlignmentOptions.TopLeft;
        
        // Position text in top-left corner
        RectTransform rectTransform = fpsText.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.anchoredPosition = new Vector2(10, -10);
        rectTransform.sizeDelta = new Vector2(200, 30);
    }
    
    // Public methods to control FPS at runtime
    public void SetTargetFPS(int newTargetFPS)
    {
        targetFPS = newTargetFPS;
        if (limitFPS)
        {
            Application.targetFrameRate = targetFPS;
        }
    }
    
    public void ToggleFPSLimit(bool enable)
    {
        limitFPS = enable;
        if (limitFPS)
        {
            Application.targetFrameRate = targetFPS;
        }
        else
        {
            Application.targetFrameRate = -1; // No limit
        }
    }
    
    public void ToggleFPSDisplay(bool show)
    {
        showFPS = show;
        if (fpsText != null)
        {
            fpsText.gameObject.SetActive(show);
        }
    }
    
    // Get current FPS
    public float GetCurrentFPS()
    {
        return fps;
    }
}