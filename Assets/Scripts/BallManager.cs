using UnityEngine;
using System.Collections.Generic;

public class BallManager : MonoBehaviour
{
    [Header("All Balls")]
    [Tooltip("Optional: assign a Ball Database asset to share ball data (including in-game sprites) with CurrentBallVisualizer in the game scene. If set, this list is used instead of All Balls below.")]
    public BallDatabase ballDatabase;
    [Tooltip("Used only when Ball Database is not set.")]
    public Ball[] allBalls;

    /// <summary>Effective ball list: from ballDatabase if assigned, otherwise allBalls.</summary>
    Ball[] Balls => (ballDatabase != null && ballDatabase.balls != null && ballDatabase.balls.Length > 0) ? ballDatabase.balls : allBalls;
    /// <summary>Number of balls (for reset loops etc.).</summary>
    public int BallCount => Balls?.Length ?? 0;

    [Header("Balls Bought")]
    public List<int> ballsBought = new List<int>();

    [Header("Current Selection")]
    public int currentBallIndex = 0;

    private static BallManager instance;
    public static BallManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<BallManager>();
            }
            return instance;
        }
    }

    /// <summary>Fired when the selected ball changes.</summary>
    public static System.Action OnSelectionChanged;

    /// <summary>Fired when a ball is purchased.</summary>
    public static System.Action OnBallPurchased;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Debug.Log("BallManager: Instance created");
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.Log("BallManager: Duplicate instance destroyed");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        currentBallIndex = PlayerPrefs.GetInt("CurrentBallIndex", 0);

        if (Balls != null && Balls.Length > 0 && currentBallIndex >= Balls.Length)
        {
            currentBallIndex = 0;
        }

        RefreshBallsBought();
    }

    public Ball GetCurrentBall()
    {
        if (Balls != null && currentBallIndex >= 0 && currentBallIndex < Balls.Length)
        {
            return Balls[currentBallIndex];
        }
        return null;
    }

    public void SetCurrentBall(int ballIndex)
    {
        if (ballIndex >= 0 && ballIndex < Balls.Length && IsBallBought(ballIndex))
        {
            currentBallIndex = ballIndex;
            PlayerPrefs.SetInt("CurrentBallIndex", currentBallIndex);
            PlayerPrefs.Save();

            Debug.Log($"Selected ball: {Balls[currentBallIndex].ballName}");
            OnSelectionChanged?.Invoke();
        }
        else if (!IsBallBought(ballIndex))
        {
            Debug.LogWarning($"Cannot select ball {ballIndex} - not bought yet!");
        }
    }

    public void SetCurrentBall(string ballName)
    {
        for (int i = 0; i < Balls.Length; i++)
        {
            if (Balls[i].ballName == ballName)
            {
                SetCurrentBall(i);
                return;
            }
        }
        Debug.LogWarning($"Ball with name '{ballName}' not found!");
    }

    public bool IsBallBought(int ballIndex)
    {
        if (ballIndex >= 0 && ballIndex < Balls.Length)
        {
            return PlayerPrefs.GetInt($"BallPurchased_{ballIndex}", Balls[ballIndex].isUnlockedByDefault ? 1 : 0) == 1;
        }
        return false;
    }

    public bool IsBallBought(string ballName)
    {
        for (int i = 0; i < Balls.Length; i++)
        {
            if (Balls[i].ballName == ballName)
            {
                return IsBallBought(i);
            }
        }
        return false;
    }

    public void BuyBall(int ballIndex)
    {
        if (ballIndex >= 0 && ballIndex < Balls.Length)
        {
            Ball ball = Balls[ballIndex];

            if (IsBallBought(ballIndex))
            {
                Debug.Log($"Ball {ball.ballName} is already bought!");
                return;
            }

            int currentGold = PlayerPrefs.GetInt("PlayerGold", 0);
            int currentDiamonds = PlayerPrefs.GetInt("PlayerDiamonds", 0);

            if (currentGold >= ball.goldPrice && currentDiamonds >= ball.diamondPrice)
            {
                currentGold -= ball.goldPrice;
                currentDiamonds -= ball.diamondPrice;

                PlayerPrefs.SetInt("PlayerGold", currentGold);
                PlayerPrefs.SetInt("PlayerDiamonds", currentDiamonds);

                PlayerPrefs.SetInt($"BallPurchased_{ballIndex}", 1);
                PlayerPrefs.Save();

                RefreshBallsBought();

                Debug.Log($"Bought ball: {ball.ballName} for {ball.goldPrice} gold and {ball.diamondPrice} diamonds");
                OnBallPurchased?.Invoke();
            }
            else
            {
                Debug.Log($"Not enough currency! Need {ball.goldPrice} gold and {ball.diamondPrice} diamonds");
            }
        }
    }

    public void BuyBall(string ballName)
    {
        for (int i = 0; i < Balls.Length; i++)
        {
            if (Balls[i].ballName == ballName)
            {
                BuyBall(i);
                return;
            }
        }
    }

    public void RefreshBallsBought()
    {
        ballsBought.Clear();
        for (int i = 0; i < Balls.Length; i++)
        {
            if (IsBallBought(i))
            {
                ballsBought.Add(i);
            }
        }
        Debug.Log($"Balls bought: {ballsBought.Count} out of {Balls.Length}");
    }
}

[System.Serializable]
public class Ball
{
    [Header("Ball Information")]
    public string ballName = "Ball 1";
    [Tooltip("Sprite used for the in-game ball when this ball is selected.")]
    public Sprite inGameSprite;

    [Header("Pricing")]
    public int goldPrice = 0;
    public int diamondPrice = 0;
    public bool isUnlockedByDefault = false;
}

/// <summary>
/// Single source of truth for ball data. Create via Assets > Create > Ball Database.
/// Assign this asset to BallManager (Home) and CurrentBallVisualizer (game) so you set sprites once.
/// </summary>
[CreateAssetMenu(fileName = "BallDatabase", menuName = "Ball Database")]
public class BallDatabase : ScriptableObject
{
    public Ball[] balls;
}

