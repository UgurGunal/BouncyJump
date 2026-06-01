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
    private bool _sessionPaused = false;
    private int _currentLevel = 0; // New: Track current level
    


    // --- Public Properties to access data ---
    public float HighestHeightReached => _highestHeightReached;
    public int CoinsCollected => _coinsCollected;
    public int PowerupsCollected => _powerupsCollected;
    public int GemsCollected => _gemsCollected;
    public float SessionDuration => _sessionDuration;
    public int CurrentLevel => _currentLevel; // New: Public property for current level

    // Calculate total earned coins based on max level reached and coins collected
    public int TotalEarnedCoins
    {
        get
        {
            if (LevelManager.Instance != null)
            {
                int maxReachedLevel = LevelManager.Instance.GetCurrentLevel(_highestHeightReached);
                return maxReachedLevel * _coinsCollected;
            }
            return _coinsCollected; // Fallback if LevelManager is not available
        }
    }

    void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad for persistence across scenes
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

    void OnEnable()
    {
        CrossSceneReferenceManager.OnPlayerReady += HandlePlayerReady;
    }

    void OnDisable()
    {
        CrossSceneReferenceManager.OnPlayerReady -= HandlePlayerReady;
    }

    void Start()
    {
        // Start session automatically if this manager is in the initial scene
        // Or call StartSession() explicitly from LevelManager/GameManager
        // StartSession(); // This might be called from LevelManager instead
        Transform player = GameplayPlayerCache.Player;
        if (player != null)
            HandlePlayerReady(player);
    }

    static void HandlePlayerReady(Transform player)
    {
        if (player != null)
            GameplayPlayerCache.SetPlayer(player);
    }

    void Update()
    {
        if (_sessionActive && !_sessionPaused)
        {
            _sessionDuration = Time.time - _sessionStartTime;

            Transform player = GameplayPlayerCache.Player;
            if (player == null)
                return;

            float currentPlayerY = player.position.y;
            if (currentPlayerY > _highestHeightReached)
                _highestHeightReached = currentPlayerY;

            if (LevelManager.Instance != null)
                _currentLevel = LevelManager.Instance.GetCurrentLevel(currentPlayerY);
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
        _sessionPaused = false;
        _currentLevel = 0; // New: Reset level on session start
        _currencySaved = false; // Reset currency saved flag for new session
    }

    private bool _currencySaved = false; // Flag to prevent double-saving
    
    public void EndSession()
    {
        if (_sessionActive && !_sessionPaused)
            _sessionDuration = Time.time - _sessionStartTime;

        _sessionActive = false;
        _sessionPaused = false;
        // Note: Currency accumulation/saving is handled by GameEndPanelUI when it shows
    }

    /// <summary>Caps height/level when the player clears the tower (no level 7+ in endgame UI).</summary>
    public void CapSessionStatsForTowerComplete(float maxWorldY, int maxLevel)
    {
        if (_highestHeightReached > maxWorldY)
            _highestHeightReached = maxWorldY;
        _currentLevel = maxLevel;
    }

    /// <summary>Freezes the run timer while the pause panel is open (resume continues from the same time).</summary>
    public void PauseSession()
    {
        if (!_sessionActive || _sessionPaused)
            return;

        _sessionDuration = Time.time - _sessionStartTime;
        _sessionPaused = true;
    }
    
    // Method to mark currency as already saved (called externally when currency is saved immediately)
    public void MarkCurrencyAsSaved()
    {
        _currencySaved = true;
    }
    
    // Method to accumulate current session's currency and save to PlayerPrefs immediately for safety
    public void AccumulateSessionCurrency()
    {
        if (!_currencySaved)
        {
            int earnedGold = TotalEarnedCoins;
            int earnedDiamonds = _gemsCollected;
            
            // Save immediately to PlayerPrefs for crash protection
            if (earnedGold > 0 || earnedDiamonds > 0)
            {
                int currentGold = PlayerPrefs.GetInt("PlayerGold", 0);
                int currentDiamonds = PlayerPrefs.GetInt("PlayerDiamonds", 0);
                
                PlayerPrefs.SetInt("PlayerGold", currentGold + earnedGold);
                PlayerPrefs.SetInt("PlayerDiamonds", currentDiamonds + earnedDiamonds);
                PlayerPrefs.Save();
                
            }
            
            _currencySaved = true; // Mark this session as processed
        }
    }

    /// <summary>
    /// Same gold/diamond persistence and tower best-height update as the game-over flow (<see cref="GameEndPanelUI"/>),
    /// for leaving mid-run (e.g. pause â†’ home). Safe if <see cref="EndSession"/> was already called (e.g. after death).
    /// </summary>
    public void FinalizeRunRewardsForMenuExit()
    {
        if (_sessionActive)
            EndSession();

        int towerIndex = TowerHeightHighScore.GetCurrentTowerIndexFromSave();
        TowerHeightHighScore.TryRecordHeight(towerIndex, HighestHeightReached);
        AccumulateSessionCurrency();
    }

    public void ResumeSession()
    {
        if (_sessionPaused)
        {
            _sessionStartTime = Time.time - _sessionDuration;
            _sessionPaused = false;
            return;
        }

        // After death/revive: continue tracking without resetting collected items
        if (!_sessionActive)
        {
            _sessionStartTime = Time.time - _sessionDuration;
            _sessionActive = true;
        }
    }

    // --- Collectable Methods ---
    public void AddCoin(int value)
    {
        _coinsCollected += value;
        // Note: Currency is saved at end of session based on TotalEarnedCoins calculation
    }

    public void AddPowerup()
    {
        _powerupsCollected++;
    }

    public void AddGem(int value)
    {
        _gemsCollected += value;
        // Note: Currency is saved at end of session
    }
}
