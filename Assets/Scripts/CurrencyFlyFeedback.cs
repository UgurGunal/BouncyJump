using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum CurrencyFlyType
{
    Gold,
    Diamond
}

/// <summary>
/// Which currency HUD to fly into. Auto picks shop when the shop panel is open, otherwise home.
/// </summary>
public enum CurrencyFlyHud
{
    Auto,
    Home,
    Shop
}

/// <summary>
/// Spawns gold/diamond icons that fly to the currency HUD, then increments the displayed
/// amount on each arrival. Wallet save can already include the full grant; this only
/// animates what the player sees. Home and shop each have their own targets and texts.
/// </summary>
public class CurrencyFlyFeedback : MonoBehaviour
{
    public static CurrencyFlyFeedback Instance { get; private set; }

    [Header("Home HUD")]
    [Tooltip("Gold icon on the home currency bar.")]
    public RectTransform homeGoldFlyTarget;
    [Tooltip("Diamond icon on the home currency bar.")]
    public RectTransform homeDiamondFlyTarget;
    public TextMeshProUGUI homeGoldAmountText;
    public TextMeshProUGUI homeDiamondAmountText;

    [Header("Shop HUD")]
    [Tooltip("Gold icon on the shop currency bar.")]
    public RectTransform shopGoldFlyTarget;
    [Tooltip("Diamond icon on the shop currency bar.")]
    public RectTransform shopDiamondFlyTarget;
    public TextMeshProUGUI shopGoldAmountText;
    public TextMeshProUGUI shopDiamondAmountText;

    [Header("Icon visuals")]
    [Tooltip("UI prefab rooted on a RectTransform (usually an Image of a gold coin).")]
    public RectTransform goldIconPrefab;
    [Tooltip("UI prefab rooted on a RectTransform (usually an Image of a diamond).")]
    public RectTransform diamondIconPrefab;
    [Tooltip("Parent for flying icons. Defaults to this transform.")]
    public RectTransform iconLayer;

    [Header("Spawn (screen center burst)")]
    [Tooltip("Minimum distance from screen center for each icon (screen pixels).")]
    public float spawnRadiusMin = 60f;
    [Tooltip("Maximum distance from screen center for each icon (screen pixels).")]
    public float spawnRadiusMax = 160f;
    [Tooltip("Extra random angle jitter in degrees so rings do not look perfect.")]
    public float spawnAngleJitter = 18f;
    [Tooltip("How long each icon waits after popping in before flying to the HUD.")]
    public float spawnHoldDuration = 0.65f;
    [Tooltip("Delay between each icon appearing in the burst.")]
    public float spawnStagger = 0.04f;
    [Tooltip("Scale icons pop in from this size during the burst.")]
    public float spawnPopFromScale = 0.25f;

    [Header("Flight")]
    public float flyDuration = 0.55f;
    [Tooltip("Random arc height added at mid-flight (screen pixels).")]
    public float arcHeight = 80f;
    [Tooltip("Scale punch on the fly target when an icon arrives.")]
    public float targetPunchScale = 1.18f;
    public float targetPunchDuration = 0.12f;

    Canvas _canvas;
    Camera _uiCamera;
    ShopManager _shopManager;
    int _displayedGold;
    int _displayedDiamonds;
    int _pendingGold;
    int _pendingDiamonds;
    bool _holdingGold;
    bool _holdingDiamonds;
    /// <summary>HUD used for the in-flight gold animation (targets + text writes).</summary>
    CurrencyFlyHud _activeGoldHud = CurrencyFlyHud.Home;
    /// <summary>HUD used for the in-flight diamond animation.</summary>
    CurrencyFlyHud _activeDiamondHud = CurrencyFlyHud.Home;
    Coroutine _goldPunchRoutine;
    Coroutine _diamondPunchRoutine;
    readonly List<GameObject> _activeIcons = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (iconLayer == null)
            iconLayer = transform as RectTransform;

        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            _uiCamera = _canvas.worldCamera;
    }

    void Start()
    {
        AutoWireHudIfNeeded();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void AutoWireHudIfNeeded()
    {
        HomeScreenCurrencyDisplay home = Object.FindObjectOfType<HomeScreenCurrencyDisplay>(true);
        if (home != null)
        {
            if (homeGoldAmountText == null)
                homeGoldAmountText = home.goldText;
            if (homeDiamondAmountText == null)
                homeDiamondAmountText = home.diamondText;
        }

        if (_shopManager == null)
            _shopManager = Object.FindObjectOfType<ShopManager>(true);

        if (_shopManager != null)
        {
            if (shopGoldAmountText == null)
                shopGoldAmountText = _shopManager.goldText;
            if (shopDiamondAmountText == null)
                shopDiamondAmountText = _shopManager.diamondText;
        }
    }

    /// <summary>
    /// True while gold icons are still flying / display is lagging behind the wallet.
    /// </summary>
    public bool TryGetDisplayedGold(out int displayed)
    {
        if (_holdingGold)
        {
            displayed = _displayedGold;
            return true;
        }

        displayed = 0;
        return false;
    }

    /// <summary>
    /// True while diamond icons are still flying / display is lagging behind the wallet.
    /// </summary>
    public bool TryGetDisplayedDiamonds(out int displayed)
    {
        if (_holdingDiamonds)
        {
            displayed = _displayedDiamonds;
            return true;
        }

        displayed = 0;
        return false;
    }

    /// <summary>
    /// Play earn FX for currency that is already in the wallet.
    /// Icons burst near screen center, then tween to the active HUD (shop if open, else home).
    /// </summary>
    public void Play(CurrencyFlyType type, int amount, CurrencyFlyHud hud = CurrencyFlyHud.Auto)
    {
        if (amount <= 0)
            return;

        AutoWireHudIfNeeded();
        CurrencyFlyHud resolved = ResolveHud(hud);

        if (type == CurrencyFlyType.Gold)
            BeginGoldHold(amount, resolved);
        else
            BeginDiamondHold(amount, resolved);

        StartCoroutine(SpawnAndFlyRoutine(type, amount, resolved));
    }

    public void PlayGold(int amount, CurrencyFlyHud hud = CurrencyFlyHud.Auto)
    {
        Play(CurrencyFlyType.Gold, amount, hud);
    }

    public void PlayDiamonds(int amount, CurrencyFlyHud hud = CurrencyFlyHud.Auto)
    {
        Play(CurrencyFlyType.Diamond, amount, hud);
    }

    CurrencyFlyHud ResolveHud(CurrencyFlyHud hud)
    {
        if (hud == CurrencyFlyHud.Home || hud == CurrencyFlyHud.Shop)
            return hud;

        if (_shopManager == null)
            _shopManager = Object.FindObjectOfType<ShopManager>(true);

        return _shopManager != null && _shopManager.IsShopOpen
            ? CurrencyFlyHud.Shop
            : CurrencyFlyHud.Home;
    }

    RectTransform GetFlyTarget(CurrencyFlyType type, CurrencyFlyHud hud)
    {
        if (type == CurrencyFlyType.Gold)
            return hud == CurrencyFlyHud.Shop ? shopGoldFlyTarget : homeGoldFlyTarget;
        return hud == CurrencyFlyHud.Shop ? shopDiamondFlyTarget : homeDiamondFlyTarget;
    }

    TextMeshProUGUI GetAmountText(CurrencyFlyType type, CurrencyFlyHud hud)
    {
        if (type == CurrencyFlyType.Gold)
            return hud == CurrencyFlyHud.Shop ? shopGoldAmountText : homeGoldAmountText;
        return hud == CurrencyFlyHud.Shop ? shopDiamondAmountText : homeDiamondAmountText;
    }

    void BeginGoldHold(int amount, CurrencyFlyHud hud)
    {
        int wallet = GameSaveService.GetGold();
        if (!_holdingGold)
        {
            _displayedGold = Mathf.Max(0, wallet - amount);
            _holdingGold = true;
            _pendingGold = 0;
            _activeGoldHud = hud;
        }

        _pendingGold += amount;
        ApplyAmountText(CurrencyFlyType.Gold, _activeGoldHud, _displayedGold);
    }

    void BeginDiamondHold(int amount, CurrencyFlyHud hud)
    {
        int wallet = GameSaveService.GetDiamonds();
        if (!_holdingDiamonds)
        {
            _displayedDiamonds = Mathf.Max(0, wallet - amount);
            _holdingDiamonds = true;
            _pendingDiamonds = 0;
            _activeDiamondHud = hud;
        }

        _pendingDiamonds += amount;
        ApplyAmountText(CurrencyFlyType.Diamond, _activeDiamondHud, _displayedDiamonds);
    }

    IEnumerator SpawnAndFlyRoutine(CurrencyFlyType type, int totalAmount, CurrencyFlyHud hud)
    {
        int iconCount = Mathf.Clamp(GetIconCountForAmount(type, totalAmount), 1, Mathf.Max(1, totalAmount));
        int baseValue = totalAmount / iconCount;
        int remainder = totalAmount % iconCount;

        RectTransform target = GetFlyTarget(type, hud);
        if (target == null)
        {
            GrantVisualChunk(type, totalAmount);
            yield break;
        }

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        float angleStep = 360f / iconCount;
        float startAngle = Random.Range(0f, 360f);
        float radiusMin = Mathf.Max(0f, spawnRadiusMin);
        float radiusMax = Mathf.Max(radiusMin, spawnRadiusMax);

        for (int i = 0; i < iconCount; i++)
        {
            int chunk = baseValue + (i < remainder ? 1 : 0);
            if (chunk <= 0)
                continue;

            float angle = (startAngle + i * angleStep + Random.Range(-spawnAngleJitter, spawnAngleJitter)) * Mathf.Deg2Rad;
            float radius = Random.Range(radiusMin, radiusMax);
            Vector2 startScreen = screenCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            StartCoroutine(FlyOneIconRoutine(type, chunk, startScreen, target, hud));

            if (spawnStagger > 0f && i < iconCount - 1)
                yield return new WaitForSecondsRealtime(spawnStagger);
        }
    }

    /// <summary>
    /// Gold: &lt;=1k → 6, &lt;=6k → 12, &lt;=45k → 18, else 24.
    /// Diamonds: &lt;=5 → 5, &lt;=50 → 12, &lt;=300 → 19, else 40.
    /// </summary>
    static int GetIconCountForAmount(CurrencyFlyType type, int amount)
    {
        if (type == CurrencyFlyType.Gold)
        {
            if (amount <= 1000) return 6;
            if (amount <= 6000) return 12;
            if (amount <= 45000) return 18;
            return 24;
        }

        if (amount <= 5) return 5;
        if (amount <= 50) return 12;
        if (amount <= 300) return 19;
        return 40;
    }

    IEnumerator FlyOneIconRoutine(CurrencyFlyType type, int chunk, Vector2 startScreen, RectTransform target, CurrencyFlyHud hud)
    {
        RectTransform icon = CreateIcon(type);
        if (icon == null)
        {
            GrantVisualChunk(type, chunk);
            yield break;
        }

        _activeIcons.Add(icon.gameObject);

        SetIconScreenPoint(icon, startScreen);
        float restScale = Random.Range(0.9f, 1.15f);
        icon.localScale = Vector3.one * Mathf.Max(0.01f, spawnPopFromScale);

        float popDuration = 0.12f;
        float popElapsed = 0f;
        while (popElapsed < popDuration)
        {
            popElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(popElapsed / popDuration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            icon.localScale = Vector3.one * Mathf.Lerp(spawnPopFromScale, restScale, ease);
            yield return null;
        }

        icon.localScale = Vector3.one * restScale;

        if (spawnHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(spawnHoldDuration);

        Vector2 endScreen = RectTransformToScreenPoint(target);
        Vector2 midScreen = Vector2.Lerp(startScreen, endScreen, 0.5f);
        midScreen += Vector2.up * Random.Range(arcHeight * 0.35f, arcHeight);
        midScreen += Random.insideUnitCircle * 40f;

        float duration = Mathf.Max(0.05f, flyDuration);
        float elapsed = 0f;
        Vector3 startScale = icon.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            Vector2 a = Vector2.Lerp(startScreen, midScreen, ease);
            Vector2 b = Vector2.Lerp(midScreen, endScreen, ease);
            Vector2 pos = Vector2.Lerp(a, b, ease);
            SetIconScreenPoint(icon, pos);

            float scaleT = t < 0.75f ? 1f : Mathf.Lerp(1f, 0.45f, (t - 0.75f) / 0.25f);
            icon.localScale = startScale * scaleT;

            yield return null;
        }

        GrantVisualChunk(type, chunk);
        PunchTarget(type, hud);

        _activeIcons.Remove(icon.gameObject);
        Destroy(icon.gameObject);
    }

    void GrantVisualChunk(CurrencyFlyType type, int chunk)
    {
        if (chunk <= 0)
            return;

        if (type == CurrencyFlyType.Gold)
        {
            _displayedGold += chunk;
            _pendingGold = Mathf.Max(0, _pendingGold - chunk);
            ApplyAmountText(CurrencyFlyType.Gold, _activeGoldHud, _displayedGold);

            if (_pendingGold <= 0)
            {
                _holdingGold = false;
                _displayedGold = GameSaveService.GetGold();
                ApplyAmountText(CurrencyFlyType.Gold, _activeGoldHud, _displayedGold);
                // Keep the other HUD in sync with the final wallet value.
                CurrencyFlyHud other = _activeGoldHud == CurrencyFlyHud.Shop ? CurrencyFlyHud.Home : CurrencyFlyHud.Shop;
                ApplyAmountText(CurrencyFlyType.Gold, other, _displayedGold);
            }
        }
        else
        {
            _displayedDiamonds += chunk;
            _pendingDiamonds = Mathf.Max(0, _pendingDiamonds - chunk);
            ApplyAmountText(CurrencyFlyType.Diamond, _activeDiamondHud, _displayedDiamonds);

            if (_pendingDiamonds <= 0)
            {
                _holdingDiamonds = false;
                _displayedDiamonds = GameSaveService.GetDiamonds();
                ApplyAmountText(CurrencyFlyType.Diamond, _activeDiamondHud, _displayedDiamonds);
                CurrencyFlyHud other = _activeDiamondHud == CurrencyFlyHud.Shop ? CurrencyFlyHud.Home : CurrencyFlyHud.Shop;
                ApplyAmountText(CurrencyFlyType.Diamond, other, _displayedDiamonds);
            }
        }
    }

    void PunchTarget(CurrencyFlyType type, CurrencyFlyHud hud)
    {
        RectTransform target = GetFlyTarget(type, hud);
        if (target == null || targetPunchScale <= 1.001f)
            return;

        if (type == CurrencyFlyType.Gold)
        {
            if (_goldPunchRoutine != null)
                StopCoroutine(_goldPunchRoutine);
            _goldPunchRoutine = StartCoroutine(PunchScaleRoutine(target, true));
        }
        else
        {
            if (_diamondPunchRoutine != null)
                StopCoroutine(_diamondPunchRoutine);
            _diamondPunchRoutine = StartCoroutine(PunchScaleRoutine(target, false));
        }
    }

    IEnumerator PunchScaleRoutine(RectTransform target, bool isGold)
    {
        Vector3 original = Vector3.one;
        float duration = Mathf.Max(0.01f, targetPunchDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float punch = t < 0.5f
                ? Mathf.Lerp(1f, targetPunchScale, t * 2f)
                : Mathf.Lerp(targetPunchScale, 1f, (t - 0.5f) * 2f);
            target.localScale = original * punch;
            yield return null;
        }

        target.localScale = original;

        if (isGold)
            _goldPunchRoutine = null;
        else
            _diamondPunchRoutine = null;
    }

    void ApplyAmountText(CurrencyFlyType type, CurrencyFlyHud hud, int amount)
    {
        TextMeshProUGUI tmp = GetAmountText(type, hud);
        if (tmp != null)
            tmp.text = FormatCurrency(amount);
    }

    static string FormatCurrency(int amount)
    {
        return amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }

    RectTransform CreateIcon(CurrencyFlyType type)
    {
        RectTransform prefab = type == CurrencyFlyType.Gold ? goldIconPrefab : diamondIconPrefab;
        RectTransform parent = iconLayer != null ? iconLayer : transform as RectTransform;
        if (prefab == null || parent == null)
            return null;

        RectTransform instance = Instantiate(prefab, parent);
        instance.gameObject.SetActive(true);
        instance.anchorMin = new Vector2(0.5f, 0.5f);
        instance.anchorMax = new Vector2(0.5f, 0.5f);
        instance.pivot = new Vector2(0.5f, 0.5f);
        instance.localRotation = Quaternion.identity;
        instance.localScale = Vector3.one;
        return instance;
    }

    Vector2 RectTransformToScreenPoint(RectTransform rect)
    {
        if (rect == null)
            return Vector2.zero;

        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) * 0.5f;

        if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return RectTransformUtility.WorldToScreenPoint(null, center);

        return RectTransformUtility.WorldToScreenPoint(_uiCamera, center);
    }

    void SetIconScreenPoint(RectTransform icon, Vector2 screenPoint)
    {
        if (icon == null)
            return;

        Camera eventCam = null;
        if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCam = _uiCamera != null ? _uiCamera : _canvas.worldCamera;

        RectTransform parent = icon.parent as RectTransform;
        if (parent == null)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, eventCam, out Vector2 local))
            icon.anchoredPosition = local;
    }
}
