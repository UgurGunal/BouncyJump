using UnityEngine;

public class PointsManager : MonoBehaviour
{
    public static PointsManager Instance { get; private set; }

    // --- Tracked Data ---
    private float _highestHeightReached = 0f;
    private int _coinsCollected = 0;
    private int _powerupsCollected = 0;
    private int _gemsCollected = 0;
    private float _sessionStartTime = 0f;
    private float _sessionDuration = 0f;
    private bool _sessionActive = false;
    private int _currentLevel = 0; // New: Track current level
    private int _totalEarnedCoins = 0;

    // --- Public Properties to access data ---
    public float HighestHeightReached => _highestHeightReached;
    public int CoinsCollected => _coinsCollected;
    public int PowerupsCollected => _powerupsCollected;
    public int GemsCollected => _gemsCollected;
    public float SessionDuration => _sessionDuration;
    public int CurrentLevel => _currentLevel; // New: Public property for current level
    public int TotalEarnedCoins => _totalEarnedCoins;

    void Awake()

    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep across scene loads if needed, or remove if manager is per-scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Start session automatically if this manager is in the initial scene
        // Or call StartSession() explicitly from LevelManager/GameManager
        // StartSession(); // This might be called from LevelManager instead
    }

    void Update()
    {
        if (_sessionActive)
        {
            // Update highest height reached
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                float currentPlayerY = playerObject.transform.position.y;
                if (currentPlayerY > _highestHeightReached)
                {
                    _highestHeightReached = currentPlayerY;
                }

                // Update current level based on player's height
                if (LevelManager.Instance != null)
                {
                    _currentLevel = LevelManager.Instance.GetCurrentLevel(currentPlayerY);
                }
            }

            // Update session duration
            _sessionDuration = Time.time - _sessionStartTime;
        }
    }

    // --- Session Management ---
    public void StartSession()
    {
        _highestHeightReached = 0f;
        _coinsCollected = 0;
        _powerupsCollected = 0;
        _gemsCollected = 0;
        _sessionStartTime = Time.time;
        _sessionDuration = 0f;
        _sessionActive = true;
        _currentLevel = 0; // New: Reset level on session start
        _totalEarnedCoins = 0;
        
    }

    public void EndSession()
    {
        _sessionActive = false;

        if (LevelManager.Instance != null)
        {
            int maxReachedLevel = Mathf.CeilToInt(_highestHeightReached / LevelManager.Instance.levelHeight);
            _totalEarnedCoins = _coinsCollected * Mathf.Max(1, maxReachedLevel);
        }
        else
        {
            _totalEarnedCoins = _coinsCollected;
        }
    }

    // --- Collectable Methods ---
    public void AddCoin(int value)
    {
        _coinsCollected += value;
        
    }

    public void AddPowerup()
    {
        _powerupsCollected++;
        
    }

    public void AddGem(int value)
    {
        _gemsCollected += value;
        
    }
}
