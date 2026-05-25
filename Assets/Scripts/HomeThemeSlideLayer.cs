using UnityEngine;

/// <summary>
/// One tower's sliding stack (sky + foreground children). Clouds must NOT be a child of this object.
/// </summary>
public class HomeThemeSlideLayer : MonoBehaviour
{
    [Tooltip("Root that slides (usually this object, e.g. BackgroundPanelA).")]
    public RectTransform slideRoot;

    public HomeThemeSkySlot sky;
    public HomeThemeForegroundView foreground;

    void Awake()
    {
        if (slideRoot == null)
            slideRoot = transform as RectTransform;
        AutoWire();
    }

    void OnValidate()
    {
        if (slideRoot == null)
            slideRoot = transform as RectTransform;
        AutoWire();
    }

    public void ApplyTower(Tower tower)
    {
        TowerHomeTheme theme = tower != null ? tower.GetResolvedHomeTheme() : null;

        if (sky != null)
            sky.ApplyTheme(theme);
        if (foreground != null)
            foreground.ApplyTheme(theme);
    }

    public void SetSlideOffset(float x)
    {
        if (slideRoot != null)
            slideRoot.anchoredPosition = new Vector2(x, 0f);
    }

    public void AutoWire()
    {
        if (slideRoot == null)
            slideRoot = transform as RectTransform;

        if (sky == null)
            sky = GetComponentInChildren<HomeThemeSkySlot>(true);

        if (foreground == null)
            foreground = GetComponentInChildren<HomeThemeForegroundView>(true);
    }
}
