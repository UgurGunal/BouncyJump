using UnityEngine;

/// <summary>
/// Updates the in-game ball sprite from the currently selected ball.
/// Prefers BallManager's live selection if available; falls back to PlayerPrefs.
/// Place this in the game scene only. Assign the same Ball Database asset used by BallManager so you set sprites once.
/// </summary>
public class CurrentBallVisualizer : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("World-space SpriteRenderer for the ball (e.g. on the Player).")]
    public SpriteRenderer targetSpriteRenderer;

    [Header("Ball data")]
    [Tooltip("Same Ball Database asset assigned to BallManager in Home scene. Sprites are read from here.")]
    public BallDatabase ballDatabase;

    void OnEnable()
    {
        ApplyCurrentBallVisual();
    }

    void ApplyCurrentBallVisual()
    {
        if (targetSpriteRenderer == null)
            return;

        // 1) Prefer live data from BallManager for this session
        if (BallManager.Instance != null)
        {
            Ball currentBall = BallManager.Instance.GetCurrentBall();
            if (currentBall != null && currentBall.inGameSprite != null)
            {
                targetSpriteRenderer.sprite = currentBall.inGameSprite;
                targetSpriteRenderer.enabled = true;
                return;
            }
        }

        // 2) Fallback to PlayerPrefs + BallDatabase (e.g. when no BallManager in this scene)
        if (ballDatabase == null || ballDatabase.balls == null || ballDatabase.balls.Length == 0)
            return;

        int index = PlayerPrefs.GetInt("CurrentBallIndex", 0);
        if (index < 0 || index >= ballDatabase.balls.Length)
            index = 0;

        Sprite sprite = ballDatabase.balls[index].inGameSprite;
        targetSpriteRenderer.sprite = sprite;
        targetSpriteRenderer.enabled = sprite != null;
    }
}
