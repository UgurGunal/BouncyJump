using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sky and foreground are separate viewport children so clouds sit between them in draw order.
/// Hierarchy (back to front): SkyA, SkyB, Clouds, FrontA, FrontB. Sky + matching foreground slide together.
/// </summary>
public class HomeTowerCarouselController : MonoBehaviour
{
    const string SlideSetAName = "SlideSetA";
    const string SlideSetBName = "SlideSetB";

    [Header("Viewport")]
    public RectTransform slideViewport;

    [Header("Sky — drag Sky1 / Sky2 GameObjects here")]
    public GameObject skyA;
    public GameObject skyB;

    [Header("Foreground — drag panel A / B GameObjects here")]
    public GameObject frontA;
    public GameObject frontB;

    [Header("Clouds (fixed — direct child of viewport, between skies and foregrounds)")]
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
    [Tooltip("Order: skies, Clouds, then foregrounds.")]
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
        RestoreFlatViewportHierarchy();
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
        RestoreFlatViewportHierarchy();
        DisableLegacyThemeController();

        if (leftButton != null)
            leftButton.onClick.AddListener(OnLeftClicked);
        if (rightButton != null)
            rightButton.onClick.AddListener(OnRightClicked);

        TowerManager.OnSelectionChanged += OnExternalTowerChanged;
        TowerManager.OnTowerPurchased += OnExternalTowerChanged;
        EnsureViewportClipping();
        EnsureSkyReferences();
        EnsureSharedClouds();
        if (enforceDrawOrder)
            ApplyViewportDrawOrder();
        ValidateSetup();
        RefreshSlideWidth();
        SnapToCurrentTower();
        UpdateNavigationButtons();
        NotifyHomeTowerLockPresentation();
    }

    static void NotifyHomeTowerLockPresentation()
    {
        HomeTowerLockPresentation presentation = Object.FindObjectOfType<HomeTowerLockPresentation>();
        presentation?.Refresh();
    }

    void OnDisable()
    {
        IsSlideControllerActive = false;

        if (leftButton != null)
            leftButton.onClick.RemoveListener(OnLeftClicked);
        if (rightButton != null)
            rightButton.onClick.RemoveListener(OnRightClicked);

        TowerManager.OnSelectionChanged -= OnExternalTowerChanged;
        TowerManager.OnTowerPurchased -= OnExternalTowerChanged;

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
    }

    /// <summary>
    /// Undo legacy SlideSetA/B grouping so Clouds can sit between skies and foregrounds.
    /// </summary>
    void RestoreFlatViewportHierarchy()
    {
        if (slideViewport == null)
            return;

        ReparentToViewport(skyA);
        ReparentToViewport(skyB);
        ReparentToViewport(frontA);
        ReparentToViewport(frontB);

        DestroyEmptySlideSet(SlideSetAName);
        DestroyEmptySlideSet(SlideSetBName);
    }

    void ReparentToViewport(GameObject child)
    {
        if (child == null || slideViewport == null)
            return;

        if (child.transform.parent != slideViewport)
            child.transform.SetParent(slideViewport, false);
    }

    void DestroyEmptySlideSet(string slideSetName)
    {
        if (slideViewport == null)
            return;

        Transform slideSet = slideViewport.Find(slideSetName);
        if (slideSet == null)
            return;

        while (slideSet.childCount > 0)
            slideSet.GetChild(0).SetParent(slideViewport, false);

        if (Application.isPlaying)
            Destroy(slideSet.gameObject);
        else
            DestroyImmediate(slideSet.gameObject);
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
        {
            ReparentToViewport(sharedClouds);
            sharedClouds.SetActive(true);
        }
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

    void ApplyViewportDrawOrder()
    {
        if (slideViewport == null)
            return;

        int index = 0;
        SetSiblingIndex(skyA, ref index);
        SetSiblingIndex(skyB, ref index);

        if (sharedClouds != null && sharedClouds.transform.parent == slideViewport)
            sharedClouds.transform.SetSiblingIndex(index++);

        SetSiblingIndex(frontA, ref index);
        SetSiblingIndex(frontB, ref index);
    }

    static void SetSiblingIndex(GameObject target, ref int index)
    {
        if (target == null)
            return;

        target.transform.SetSiblingIndex(index);
        index++;
    }

    void ValidateSetup()
    {
        if (slideViewport == null)
            return;

        if (skyA != null && skyA.transform.parent != slideViewport)
        {
            Debug.LogWarning(
                "HomeTowerCarousel: Sky A should be a direct child of the viewport (not inside a slide set or panel).");
        }

        if (frontA != null && frontA.transform.parent != slideViewport)
        {
            Debug.LogWarning(
                "HomeTowerCarousel: Front A should be a direct child of the viewport.");
        }

        if (!GetFrontSet().IsValid || !GetBackSet().IsValid)
            Debug.LogWarning("HomeTowerCarousel: Assign Sky A/B and Front A/B on the carousel.");
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
        NotifyHomeTowerLockPresentation();
    }

    void OnLeftClicked()
    {
        if (isAnimating)
            return;

        TowerManager tm = TowerManagerRef;
        if (tm == null || tm.allTowers == null || tm.allTowers.Length == 0)
            return;

        int prev = TowerManager.WrapTowerIndex(tm.currentTowerIndex - 1, tm.allTowers.Length);
        PlayTowerSwipeSound();
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
        PlayTowerSwipeSound();
        slideRoutine = StartCoroutine(SlideToTower(next, fromRight: true));
    }

    void PlayTowerSwipeSound()
    {
        if (SoundEffectsManager.Instance != null)
            SoundEffectsManager.Instance.PlayHomeTowerSwipeSound();
    }

    IEnumerator SlideToTower(int towerIndex, bool fromRight)
    {
        isAnimating = true;
        SetNavigationInteractable(false);
        RefreshSlideWidth();

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

        EnsureBothSetsActive();
        Canvas.ForceUpdateCanvases();

        back.ApplyTower(tower);
        back.SetOffset(incomingStartX);
        front.SetOffset(0f);

        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, slideDuration);

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

        tm?.SelectHomeTowerVisual(towerIndex);

        isAnimating = false;
        slideRoutine = null;

        SetNavigationInteractable(true);
        UpdateNavigationButtons();
        NotifyHomeTowerLockPresentation();
    }

    void SnapToCurrentTower()
    {
        TowerManager tm = TowerManagerRef;
        Tower tower = tm != null ? tm.GetCurrentTower() : null;

        TowerSlideSet front = GetFrontSet();
        front.ApplyTower(tower);
        front.SetOffset(0f);
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
        TowerSlideSet front = GetFrontSet();
        TowerSlideSet back = GetBackSet();

        SetSetActive(front, true);
        SetSetActive(back, false);
        front.SetOffset(0f);
        back.SetOffset(0f);
    }

    void EnsureBothSetsActive()
    {
        SetSetActive(GetFrontSet(), true);
        SetSetActive(GetBackSet(), true);
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
