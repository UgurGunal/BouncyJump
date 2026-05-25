using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sky and foreground are separate viewport children so clouds can sit between them in draw order.
/// Hierarchy (top = behind): SkyA, SkyB, Clouds, FrontA, FrontB.
/// </summary>
public class HomeTowerCarouselController : MonoBehaviour
{
    [Header("Viewport")]
    public RectTransform slideViewport;

    [Header("Sky — drag Sky1 / Sky2 GameObjects here")]
    public GameObject skyA;
    public GameObject skyB;

    [Header("Foreground — drag panel A / B GameObjects here")]
    public GameObject frontA;
    public GameObject frontB;

    [Header("Clouds (fixed — direct child of viewport, between sky and front)")]
    public GameObject sharedClouds;

    [Header("Optional auto-wire")]
    public HomeThemeSlideLayer slideLayerA;
    public HomeThemeSlideLayer slideLayerB;

    [Header("Navigation")]
    public Button leftButton;
    public Button rightButton;

    [Header("Animation")]
    public float slideDuration = 0.35f;
    public bool clipToViewport = true;
    [Tooltip("Places Clouds between sky and foreground layers without moving them.")]
    public bool enforceDrawOrder = true;

    [Header("Optional")]
    public TowerManager towerManager;

    TowerManager TowerManagerRef
    {
        get
        {
            if (towerManager == null)
                towerManager = TowerManager.Instance;
            return towerManager;
        }
    }

    bool setAIsFront = true;
    bool isAnimating;
    Coroutine slideRoutine;
    float slideWidth = 1080f;

    HomeThemeSkySlot skySlotA;
    HomeThemeSkySlot skySlotB;
    HomeThemeForegroundView foregroundA;
    HomeThemeForegroundView foregroundB;

    public static bool IsSlideControllerActive { get; private set; }

    void Awake()
    {
        ResolveSlideViewport();
        AutoWireReferences();
        EnsureSkyReferences();
        ResolveSlotComponents(allowAddComponents: true);
    }

    void ResolveSlotComponents(bool allowAddComponents)
    {
        skySlotA = ResolveSkySlot(skyA, allowAddComponents);
        skySlotB = ResolveSkySlot(skyB, allowAddComponents);
        foregroundA = ResolveForegroundSlot(frontA, allowAddComponents);
        foregroundB = ResolveForegroundSlot(frontB, allowAddComponents);
    }

    static HomeThemeSkySlot ResolveSkySlot(GameObject root, bool allowAddComponents)
    {
        if (root == null)
            return null;

        HomeThemeSkySlot slot = root.GetComponent<HomeThemeSkySlot>();
        if (slot == null && allowAddComponents)
            slot = root.AddComponent<HomeThemeSkySlot>();

        if (slot != null && slot.skyImage == null)
        {
            slot.skyImage = root.GetComponent<Image>();
            if (slot.skyImage == null)
                slot.skyImage = root.GetComponentInChildren<Image>(true);
        }

        return slot;
    }

    static HomeThemeForegroundView ResolveForegroundSlot(GameObject root, bool allowAddComponents)
    {
        if (root == null)
            return null;

        HomeThemeForegroundView view = root.GetComponent<HomeThemeForegroundView>();
        if (view != null)
            return view;

        HomeThemeSlideLayer layer = root.GetComponent<HomeThemeSlideLayer>();
        if (layer != null)
        {
            layer.AutoWire();
            if (layer.foreground != null)
                return layer.foreground;
        }

        view = root.GetComponentInChildren<HomeThemeForegroundView>(true);
        if (view != null)
            return view;

        if (!allowAddComponents)
            return null;

        return root.AddComponent<HomeThemeForegroundView>();
    }

    void OnEnable()
    {
        IsSlideControllerActive = true;
        AutoWireReferences();
        ResolveSlotComponents(allowAddComponents: true);
        DisableLegacyThemeController();

        if (leftButton != null)
            leftButton.onClick.AddListener(OnLeftClicked);
        if (rightButton != null)
            rightButton.onClick.AddListener(OnRightClicked);

        TowerManager.OnSelectionChanged += OnExternalTowerChanged;
        EnsureViewportClipping();
        EnsureSkyReferences();
        EnsureSharedClouds();
        if (enforceDrawOrder)
            ApplyViewportDrawOrder();
        ValidateSetup();
        RefreshSlideWidth();
        SnapToCurrentTower();
        UpdateNavigationButtons();
    }

    void OnDisable()
    {
        IsSlideControllerActive = false;

        if (leftButton != null)
            leftButton.onClick.RemoveListener(OnLeftClicked);
        if (rightButton != null)
            rightButton.onClick.RemoveListener(OnRightClicked);

        TowerManager.OnSelectionChanged -= OnExternalTowerChanged;

        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
            slideRoutine = null;
        }

        isAnimating = false;
    }

    void AutoWireReferences()
    {
        if (skyA == null && slideLayerA != null && slideLayerA.sky != null)
            skyA = slideLayerA.sky.gameObject;
        if (skyB == null && slideLayerB != null && slideLayerB.sky != null)
            skyB = slideLayerB.sky.gameObject;

        if (frontA == null && slideLayerA != null && slideLayerA.foreground != null)
            frontA = slideLayerA.foreground.gameObject;
        if (frontB == null && slideLayerB != null && slideLayerB.foreground != null)
            frontB = slideLayerB.foreground.gameObject;
    }

    void ResolveSlideViewport()
    {
        if (slideViewport != null)
            return;

        if (skyA != null)
            slideViewport = skyA.transform.parent as RectTransform;
        else if (frontA != null)
            slideViewport = frontA.transform.parent as RectTransform;
        else if (slideLayerA != null)
            slideViewport = slideLayerA.transform.parent as RectTransform;
    }

    void EnsureSharedClouds()
    {
        if (sharedClouds == null && slideViewport != null)
        {
            Transform found = slideViewport.Find("Clouds");
            if (found != null)
                sharedClouds = found.gameObject;
        }

        if (sharedClouds != null)
            sharedClouds.SetActive(true);
    }

    void EnsureSkyReferences()
    {
        if (slideViewport == null)
            return;

        if (skyA == null)
            skyA = FindViewportChild("Sky1", "SkyA", "Sky A");
        if (skyB == null)
            skyB = FindViewportChild("Sky2", "SkyB", "Sky B");
    }

    GameObject FindViewportChild(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform found = slideViewport.Find(names[i]);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    /// <summary>
    /// Only moves Clouds — sky and foreground hierarchy order is left as set in the Editor.
    /// Unity UI: lower sibling index = drawn behind.
    /// </summary>
    void ApplyViewportDrawOrder()
    {
        if (slideViewport == null || sharedClouds == null)
            return;

        if (sharedClouds.transform.parent != slideViewport)
            return;

        int cloudsIndex = GetCloudsSiblingIndex();
        sharedClouds.transform.SetSiblingIndex(cloudsIndex);
    }

    int GetCloudsSiblingIndex()
    {
        int afterSky = -1;
        ConsiderSiblingIndex(skyA, ref afterSky, takeMax: true);
        ConsiderSiblingIndex(skyB, ref afterSky, takeMax: true);

        int beforeFront = slideViewport.childCount;
        ConsiderSiblingIndex(frontA, ref beforeFront, takeMax: false);
        ConsiderSiblingIndex(frontB, ref beforeFront, takeMax: false);

        if (afterSky < 0 && beforeFront >= slideViewport.childCount)
            return sharedClouds.transform.GetSiblingIndex();

        if (afterSky < 0)
            return Mathf.Max(0, beforeFront - 1);

        if (beforeFront >= slideViewport.childCount)
            return Mathf.Min(afterSky + 1, slideViewport.childCount - 1);

        return Mathf.Clamp(afterSky + 1, 0, beforeFront);
    }

    void ConsiderSiblingIndex(GameObject target, ref int value, bool takeMax)
    {
        if (target == null || target.transform.parent != slideViewport)
            return;

        int index = target.transform.GetSiblingIndex();
        value = takeMax ? Mathf.Max(value, index) : Mathf.Min(value, index);
    }

    void ValidateSetup()
    {
        if (slideViewport == null)
            return;

        if (skyA != null && skyA.transform.parent != slideViewport)
        {
            Debug.LogWarning(
                "HomeTowerCarousel: Sky A should be a direct child of HomeSlideViewport (not inside BackgroundPanel). " +
                "Move the Sky object out of the panel so clouds can render between sky and tower.");
        }

        if (frontA != null && frontA.transform.parent != slideViewport)
        {
            Debug.LogWarning(
                "HomeTowerCarousel: Front A should be a direct child of HomeSlideViewport (e.g. rename BackgroundPanelA and remove Sky from inside it).");
        }
    }

    void DisableLegacyThemeController()
    {
        HomeScreenThemeController legacy = GetComponent<HomeScreenThemeController>();
        if (legacy != null)
            legacy.enabled = false;
    }

    void OnExternalTowerChanged()
    {
        if (isAnimating)
            return;

        SnapToCurrentTower();
        UpdateNavigationButtons();
    }

    void OnLeftClicked()
    {
        if (isAnimating)
            return;

        TowerManager tm = TowerManagerRef;
        if (tm == null || tm.allTowers == null || tm.allTowers.Length == 0)
            return;

        int prev = TowerManager.WrapTowerIndex(tm.currentTowerIndex - 1, tm.allTowers.Length);
        slideRoutine = StartCoroutine(SlideToTower(prev, fromRight: false));
    }

    void OnRightClicked()
    {
        if (isAnimating)
            return;

        TowerManager tm = TowerManagerRef;
        if (tm == null || tm.allTowers == null || tm.allTowers.Length == 0)
            return;

        int next = TowerManager.WrapTowerIndex(tm.currentTowerIndex + 1, tm.allTowers.Length);
        slideRoutine = StartCoroutine(SlideToTower(next, fromRight: true));
    }

    IEnumerator SlideToTower(int towerIndex, bool fromRight)
    {
        isAnimating = true;
        SetNavigationInteractable(false);

        TowerManager tm = TowerManagerRef;
        Tower tower = tm != null && towerIndex >= 0 && towerIndex < tm.allTowers.Length
            ? tm.allTowers[towerIndex]
            : null;

        TowerSlideSet front = GetFrontSet();
        TowerSlideSet back = GetBackSet();

        if (!front.IsValid || !back.IsValid)
        {
            tm?.SelectHomeTowerVisual(towerIndex);
            isAnimating = false;
            SetNavigationInteractable(true);
            UpdateNavigationButtons();
            yield break;
        }

        float width = slideWidth;
        float incomingStartX = fromRight ? width : -width;
        float outgoingEndX = fromRight ? -width : width;

        SetSetActive(back, true);
        SetSetActive(front, true);

        back.ApplyTower(tower);
        back.SetOffset(incomingStartX);
        front.SetOffset(0f);

        tm?.SelectHomeTowerVisual(towerIndex);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, slideDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            back.SetOffset(Mathf.Lerp(incomingStartX, 0f, t));
            front.SetOffset(Mathf.Lerp(0f, outgoingEndX, t));

            yield return null;
        }

        back.SetOffset(0f);
        front.SetOffset(outgoingEndX);

        setAIsFront = !setAIsFront;
        ApplyFrontSetOnly();

        isAnimating = false;
        slideRoutine = null;

        SetNavigationInteractable(true);
        UpdateNavigationButtons();
    }

    void SnapToCurrentTower()
    {
        TowerManager tm = TowerManagerRef;
        Tower tower = tm != null ? tm.GetCurrentTower() : null;

        TowerSlideSet front = GetFrontSet();
        front.ApplyTower(tower);
        front.SetOffset(0f);

        GetBackSet().SetOffset(slideWidth);
        ApplyFrontSetOnly();
    }

    struct TowerSlideSet
    {
        public HomeThemeSkySlot sky;
        public HomeThemeForegroundView foreground;

        public bool IsValid => sky != null && foreground != null;

        public void ApplyTower(Tower tower)
        {
            sky?.ApplyTower(tower);
            foreground?.ApplyTower(tower);
        }

        public void SetOffset(float x)
        {
            sky?.SetSlideOffset(x);
            foreground?.SetSlideOffset(x);
        }
    }

    TowerSlideSet GetFrontSet()
    {
        return setAIsFront
            ? new TowerSlideSet { sky = skySlotA, foreground = foregroundA }
            : new TowerSlideSet { sky = skySlotB, foreground = foregroundB };
    }

    TowerSlideSet GetBackSet()
    {
        return setAIsFront
            ? new TowerSlideSet { sky = skySlotB, foreground = foregroundB }
            : new TowerSlideSet { sky = skySlotA, foreground = foregroundA };
    }

    void ApplyFrontSetOnly()
    {
        SetSetActive(GetFrontSet(), true);
        SetSetActive(GetBackSet(), false);
        GetFrontSet().SetOffset(0f);
    }

    static void SetSetActive(TowerSlideSet set, bool active)
    {
        if (set.sky != null)
            set.sky.gameObject.SetActive(active);
        if (set.foreground != null)
            set.foreground.gameObject.SetActive(active);
    }

    void EnsureViewportClipping()
    {
        if (!clipToViewport || slideViewport == null)
            return;

        RectMask2D mask = slideViewport.GetComponent<RectMask2D>();
        if (mask == null)
            mask = slideViewport.gameObject.AddComponent<RectMask2D>();

        mask.enabled = true;
    }

    void RefreshSlideWidth()
    {
        if (slideViewport != null)
            slideWidth = Mathf.Max(1f, slideViewport.rect.width);
        else
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.rootCanvas != null)
            {
                RectTransform root = canvas.rootCanvas.GetComponent<RectTransform>();
                if (root != null)
                    slideWidth = Mathf.Max(1f, root.rect.width);
            }
        }
    }

    void SetNavigationInteractable(bool interactable)
    {
        if (leftButton != null)
            leftButton.interactable = interactable;
        if (rightButton != null)
            rightButton.interactable = interactable;
    }

    void UpdateNavigationButtons()
    {
        TowerManager tm = TowerManagerRef;
        bool canNavigate = tm != null && tm.allTowers != null && tm.allTowers.Length > 1;
        SetNavigationInteractable(canNavigate && !isAnimating);
    }
}
