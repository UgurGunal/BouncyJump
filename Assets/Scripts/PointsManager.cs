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

    // --- Public Properties to access data ---
    public float HighestHeightReached => _highestHeightReached;
    public int CoinsCollected => _coinsCollected;
    public int PowerupsCollected => _powerupsCollected;
    public int GemsCollected => _gemsCollected;
    public float SessionDuration => _sessionDuration;

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
            // Assuming player is the object with PlayerBallController
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                float currentPlayerY = playerObject.transform.position.y;
                if (currentPlayerY > _highestHeightReached)
                {
                    _highestHeightReached = currentPlayerY;
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
        Debug.Log("Session Started!");
    }

    public void EndSession()
    {
        _sessionActive = false;
        Debug.Log($"Session Ended! Height: {_highestHeightReached:F2}, Coins: {_coinsCollected}, Powerups: {_powerupsCollected}, Gems: {_gemsCollected}, Time: {_sessionDuration:F2}s");
        // You might want to save these stats here or pass them to a UI
    }

    // --- Collectable Methods ---
    public void AddCoin(int value)
    {
        _coinsCollected += value;
        Debug.Log($"Coin collected! Value: {value}. Total Coins: {_coinsCollected}");
    }

    public void AddPowerup()
    {
        _powerupsCollected++;
        Debug.Log($"Powerup collected! Total Powerups: {_powerupsCollected}");
    }

    public void AddGem(int value)
    {
        _gemsCollected += value;
        Debug.Log($"Gem collected! Value: {value}. Total Gems: {_gemsCollected}");
    }
}
