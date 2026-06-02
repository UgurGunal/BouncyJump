using UnityEngine;
using System.Collections.Generic;

public class BallManager : MonoBehaviour
{
    [Header("Balls (no .asset at runtime)")]
    [Tooltip("Main list of balls. Assign balls directly here (Inspector) so Android doesn't depend on BallDatabase assets.")]
    public Ball[] balls;

    [Header("Optional migration")]
    [Tooltip("Optional: keep your old BallDatabase assigned so OnValidate can copy its balls into the 'balls' array above.")]
    public BallDatabase ballDatabase;

    private static readonly Ball[] EmptyBalls = new Ball[0];

    /// <summary>Effective ball list (prefers serialized 'balls' array).</summary>
    Ball[] Balls
    {
        get
        {
            if (balls != null && balls.Length > 0)
                return balls;

            // Migration fallback for Editor convenience (Android will still work as long as 'balls' is filled).
            if (ballDatabase != null && ballDatabase.balls != null && ballDatabase.balls.Length > 0)
                return ballDatabase.balls;

            return EmptyBalls;
        }
    }

    /// <summary>Number of balls (for reset loops etc.).</summary>
    public int BallCount => Balls.Length;

    [Header("Balls Bought")]
    public List<int> ballsBought = new List<int>();

    [Header("Current Selection")]
    public int currentBallIndex = 0;

    [Header("Save Data")]
    [Tooltip("If you change the ball list/order, bump this value to reset purchase keys so only defaults are unlocked.")]
    public int ballShopSaveVersion = 1;

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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (BallCount <= 0)
        {
        }

        int defaultUnlockedIndex = GetFirstUnlockedByDefaultIndex();

        // Reset old purchase keys when ball config changes, so only defaults are unlocked.
        GameSaveService.EnsureLoaded();

        int savedVersion = GameSaveService.GetBallShopSaveVersion();
        if (savedVersion != ballShopSaveVersion)
        {
            ResetBallPurchaseKeys();
            GameSaveService.SetBallShopSaveVersion(ballShopSaveVersion);
        }

        currentBallIndex = GameSaveService.GetCurrentBallIndex();
        if (currentBallIndex < 0 || currentBallIndex >= BallCount)
            currentBallIndex = defaultUnlockedIndex;

        if (!IsBallBought(currentBallIndex))
            currentBallIndex = defaultUnlockedIndex;

        GameSaveService.SetCurrentBallIndex(currentBallIndex);

        RefreshBallsBought();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Copy from the old BallDatabase only when the balls array isn't correctly populated.
        if (ballDatabase == null || ballDatabase.balls == null || ballDatabase.balls.Length == 0)
            return;

        bool needsCopy = (balls == null || balls.Length == 0);
        if (!needsCopy && balls.Length != ballDatabase.balls.Length)
            needsCopy = true;

        // If any element is null, treat the array as incomplete.
        if (!needsCopy)
        {
            int checkCount = Mathf.Min(balls.Length, ballDatabase.balls.Length);
            for (int i = 0; i < checkCount; i++)
            {
                if (balls[i] == null)
                {
                    needsCopy = true;
                    break;
                }
            }
        }

        if (needsCopy)
        {
            balls = ballDatabase.balls;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    [ContextMenu("Copy Balls From BallDatabase")]
    private void CopyBallsFromBallDatabase()
    {
        if (ballDatabase == null || ballDatabase.balls == null)
            return;

        balls = ballDatabase.balls;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

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
            GameSaveService.SetCurrentBallIndex(currentBallIndex);
            OnSelectionChanged?.Invoke();
        }
        else if (!IsBallBought(ballIndex))
        {
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
    }

    public bool IsBallBought(int ballIndex)
    {
        if (ballIndex >= 0 && ballIndex < Balls.Length)
        {
            // Default-unlocked balls are ALWAYS available, even if old PlayerPrefs exists.
            if (Balls[ballIndex] != null && Balls[ballIndex].isUnlockedByDefault)
                return true;

            return GameSaveService.IsBallPurchased(ballIndex);
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
                return;
            }

            int currentGold = GameSaveService.GetGold();
            int currentDiamonds = GameSaveService.GetDiamonds();

            if (currentGold >= ball.goldPrice && currentDiamonds >= ball.diamondPrice)
            {
                GameSaveService.SetGold(currentGold - ball.goldPrice);
                GameSaveService.SetDiamonds(currentDiamonds - ball.diamondPrice);
                GameSaveService.SetBallPurchased(ballIndex, true);

                RefreshBallsBought();

                if (SoundEffectsManager.Instance != null)
                    SoundEffectsManager.Instance.PlayShopPurchaseSound();

                OnBallPurchased?.Invoke();
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
    }

    private int GetFirstUnlockedByDefaultIndex()
    {
        for (int i = 0; i < BallCount; i++)
        {
            if (Balls[i] != null && Balls[i].isUnlockedByDefault)
                return i;
        }
        return 0;
    }

    private void ResetBallPurchaseKeys()
    {
        GameSaveService.ClearBallPurchases();
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

