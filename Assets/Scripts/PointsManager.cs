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
                int maxReachedLevel = Mathf.CeilToInt(_highestHeightReached / LevelManager.Instance.levelHeight);
                return Mathf.Max(1, maxReachedLevel) * _coinsCollected;
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
        _currencySaved = false; // Reset currency saved flag for new session
    }

    private bool _currencySaved = false; // Flag to prevent double-saving
    
    public void EndSession()
    {
        _sessionActive = false;
        // Note: Currency accumulation/saving is handled by GameEndPanelUI when it shows
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
        // Resume session without resetting collected items
        _sessionStartTime = Time.time - _sessionDuration; // Adjust start time to maintain continuous duration
        _sessionActive = true;
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
