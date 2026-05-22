using UnityEngine;
using UnityEngine.UI;

public class BallBuyButton : MonoBehaviour
{
    [Header("Ball Settings")]
    public string ballName = "DefaultBall"; // Must match ball name in BallManager
    public int ballIndex = 0;               // Index of this ball in BallManager.balls

    [Header("UI References")]
    [Tooltip("The buy/select button. Image is auto-resolved from this Button's GameObject.")]
    public Button buyButton;

    [Header("Button Images")]
    public Sprite originalBuyButtonSprite; // For not bought balls
    public Sprite selectButtonSprite;      // For bought but not selected balls
    public Sprite selectedButtonSprite;    // For the currently selected ball

    [Header("Background")]
    [Tooltip("Optional background image under this ball item that reflects selected vs not-selected state.")]
    public Image backgroundImage;
    [Tooltip("Background sprite when ball is NOT selected (covers both not bought and bought-but-not-selected).")]
    public Sprite backgroundNormalSprite;
    [Tooltip("Background sprite when ball IS selected.")]
    public Sprite backgroundSelectedSprite;

    [Header("Visual Effects")]
    public Color selectedTintColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public Color normalTintColor = Color.white;

    private BallManager ballManager;
    private ShopManager shopManager;
    private Image buttonImage;

    void Start()
    {
        ballManager = BallManager.Instance;
        shopManager = FindObjectOfType<ShopManager>();

        if (buyButton != null)
        {
            buttonImage = buyButton.GetComponent<Image>();
            if (buttonImage == null && buyButton.targetGraphic != null)
                buttonImage = buyButton.targetGraphic as Image;
            if (buttonImage == null)
                buttonImage = buyButton.GetComponentInChildren<Image>();
            if (originalBuyButtonSprite == null && buttonImage != null)
                originalBuyButtonSprite = buttonImage.sprite;
        }

        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnButtonClicked);
        }

        UpdateButtonState();
    }

    void OnEnable()
    {
        BallManager.OnSelectionChanged += RefreshButton;
        BallManager.OnBallPurchased += RefreshButton;
    }

    void OnDisable()
    {
        BallManager.OnSelectionChanged -= RefreshButton;
        BallManager.OnBallPurchased -= RefreshButton;
    }

    void OnButtonClicked()
    {
        if (ballManager == null)
        {
            return;
        }

        bool isBought = ballManager.IsBallBought(ballIndex);
        bool isSelected = (ballManager.currentBallIndex == ballIndex);

        if (!isBought)
        {
            ballManager.BuyBall(ballIndex);
        }
        else if (isBought && !isSelected)
        {
            ballManager.SetCurrentBall(ballIndex);
            // Selection visual will refresh via BallManager events.
        }
        else if (isSelected)
        {
            return;
        }

        if (shopManager != null)
        {
            shopManager.UpdateShopUI();
        }
    }

    void UpdateButtonState()
    {
        if (ballManager == null || buyButton == null || buttonImage == null) return;

        bool isBought = ballManager.IsBallBought(ballIndex);
        bool isSelected = (ballManager.currentBallIndex == ballIndex);

        Sprite buySprite = originalBuyButtonSprite;
        Sprite selectSprite = selectButtonSprite;
        Sprite selectedSprite = selectedButtonSprite;

        if (buttonImage != null)
        {
            if (isBought && isSelected && selectedSprite != null)
            {
                buttonImage.sprite = selectedSprite;
                buttonImage.color = selectedTintColor;
                buyButton.interactable = true;
            }
            else if (isBought && !isSelected && selectSprite != null)
            {
                buttonImage.sprite = selectSprite;
                buttonImage.color = normalTintColor;
                buyButton.interactable = true;
            }
            else if (!isBought && buySprite != null)
            {
                buttonImage.sprite = buySprite;
                buttonImage.color = normalTintColor;
                buyButton.interactable = true;
            }
            else
            {
                buttonImage.color = normalTintColor;
                buyButton.interactable = true;
            }
        }

        // Update optional background image with a simple selected / not-selected state
        if (backgroundImage != null)
        {
            if (isBought && isSelected && backgroundSelectedSprite != null)
            {
                backgroundImage.sprite = backgroundSelectedSprite;
            }
            else if (backgroundNormalSprite != null)
            {
                backgroundImage.sprite = backgroundNormalSprite;
            }
        }
    }

    public void RefreshButton()
    {
        UpdateButtonState();
    }
}

