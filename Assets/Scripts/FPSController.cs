using UnityEngine;
using TMPro;

public class FPSController : MonoBehaviour
{
    public static FPSController Instance;

    [Header("FPS Settings")]
    [SerializeField] private int targetFPS = 60;
    [SerializeField] private bool enableVSync = false;
    [SerializeField] private bool enableOnStart = true;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private float updateInterval = 0.2f;

    private float timeAccumulator;
    private int frames;
    private float currentFps;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (enableOnStart)
        {
            SetTargetFPS(targetFPS);
        }
    }

    void Update()
    {
        timeAccumulator += Time.unscaledDeltaTime;
        frames++;

        if (timeAccumulator >= updateInterval)
        {
            currentFps = frames / timeAccumulator;

            if (fpsText != null)
            {
                fpsText.text = $"{currentFps:0} FPS";
            }

            timeAccumulator = 0f;
            frames = 0;
        }
    }

    public void SetTargetFPS(int fps)
    {
        targetFPS = Mathf.Clamp(fps, 30, 120);

        if (enableVSync)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFPS;
        }

    }

    public void SetVSync(bool enable)
    {
        enableVSync = enable;
        SetTargetFPS(targetFPS);
    }

    public int GetTargetFPS()
    {
        return targetFPS;
    }

    public float GetCurrentFPS()
    {
        return currentFps;
    }

    public void ResetToDefault()
    {
        targetFPS = 60;
        enableVSync = true;
        SetTargetFPS(targetFPS);
    }

    public void SetMobileOptimized()
    {
        targetFPS = 30;
        enableVSync = true;
        SetTargetFPS(targetFPS);
    }

    public void SetHighPerformance()
    {
        targetFPS = 60;
        enableVSync = true;
        SetTargetFPS(targetFPS);
    }

    public void SetUltraPerformance()
    {
        targetFPS = 90;
        enableVSync = false;
        SetTargetFPS(targetFPS);
    }
}
