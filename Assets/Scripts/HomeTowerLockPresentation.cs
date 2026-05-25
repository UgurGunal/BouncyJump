using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// When previewing an unbought tower on home: black tint on the slide viewport only, Play disabled with a dark (opaque) look.
/// </summary>
public class HomeTowerLockPresentation : MonoBehaviour
{
    [Header("References")]
    public TowerManager towerManager;
    public Button playButton;
    public RectTransform viewportTintRoot;

    [Tooltip("Black tint over the tower background viewport. Created at runtime if empty.")]
    public Image viewportTintOverlay;

    [Header("Viewport tint")]
    [Range(0.2f, 0.85f)]
    public float viewportBlackTintAlpha = 0.55f;

    [Header("Play button (locked)")]
    public Color lockedPlayButtonColor = new Color(0.82f, 0.82f, 0.82f, 1f);

    Image playButtonImage;
    Color normalPlayButtonColor = Color.white;
    ColorBlock normalPlayColorBlock;

    void Awake()
    {
        if (towerManager == null)
            towerManager = TowerManager.Instance;

        if (playButton == null)
        {
            HomeScreenUI homeUi = FindObjectOfType<HomeScreenUI>();
            if (homeUi != null)
                playButton = homeUi.playButton;
        }

        CachePlayButtonVisuals();
        EnsureViewportTintRoot();
        EnsureViewportOverlay();
    }

    void OnEnable()
    {
        TowerManager.OnSelectionChanged += Refresh;
        TowerManager.OnTowerPurchased += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        TowerManager.OnSelectionChanged -= Refresh;
        TowerManager.OnTowerPurchased -= Refresh;
    }

    void Start()
    {
        Refresh();
    }

    void CachePlayButtonVisuals()
    {
        if (playButton == null)
            return;

        normalPlayColorBlock = playButton.colors;

        playButtonImage = playButton.targetGraphic as Image;
        if (playButtonImage == null)
            playButtonImage = playButton.GetComponent<Image>();

        if (playButtonImage != null)
            normalPlayButtonColor = playButtonImage.color;
    }

    void EnsureViewportTintRoot()
    {
        if (viewportTintRoot != null)
            return;

        HomeTowerCarouselController carousel = FindObjectOfType<HomeTowerCarouselController>();
        if (carousel != null)
            viewportTintRoot = carousel.slideViewport;
    }

    void EnsureViewportOverlay()
    {
        if (viewportTintOverlay != null || viewportTintRoot == null)
            return;

        GameObject overlayObject = new GameObject("LockedBlackTint", typeof(RectTransform), typeof(Image));
        overlayObject.transform.SetParent(viewportTintRoot, false);

        RectTransform rect = overlayObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsLastSibling();

        viewportTintOverlay = overlayObject.GetComponent<Image>();
        viewportTintOverlay.color = new Color(0f, 0f, 0f, viewportBlackTintAlpha);
        viewportTintOverlay.raycastTarget = false;
        overlayObject.SetActive(false);
    }

    public void Refresh()
    {
        if (towerManager == null)
            towerManager = TowerManager.Instance;

        bool isLocked = towerManager != null &&
                        !towerManager.IsTowerBought(towerManager.currentTowerIndex);

        if (viewportTintOverlay != null)
        {
            viewportTintOverlay.color = new Color(0f, 0f, 0f, viewportBlackTintAlpha);
            viewportTintOverlay.gameObject.SetActive(isLocked);
        }

        ApplyPlayButtonState(isLocked);
    }

    void ApplyPlayButtonState(bool isLocked)
    {
        if (playButton == null)
            return;

        if (isLocked)
        {
            playButton.interactable = false;

            ColorBlock colors = playButton.colors;
            colors.disabledColor = lockedPlayButtonColor;
            colors.fadeDuration = 0f;
            playButton.colors = colors;

            if (playButtonImage != null)
                playButtonImage.color = lockedPlayButtonColor;
        }
        else
        {
            playButton.interactable = true;
            playButton.colors = normalPlayColorBlock;

            if (playButtonImage != null)
                playButtonImage.color = normalPlayButtonColor;
        }
    }
}
