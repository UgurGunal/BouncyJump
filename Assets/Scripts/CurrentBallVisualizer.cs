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

        // 2) No BallManager available: can't resolve sprite without ball data.
        // This should not happen because BallManager is persisted from the Home scene.
    }
}
