using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single-panel home theme (no slide). Disabled when <see cref="HomeTowerCarouselController"/> is active.
/// For carousel, use <see cref="HomeThemeSkySlot"/> and <see cref="HomeThemeForegroundView"/>.
/// </summary>
public class HomeScreenThemeController : MonoBehaviour
{
    [Header("Sky (full screen)")]
    public Image skyImage;

    [Header("Tower")]
    public Image towerImage;

    [Header("Ground layers (optional)")]
    public Image[] groundImages;

    [Header("Background layers (optional)")]
    public Image[] backgroundImages;

    [Header("Decoration layers (optional)")]
    public Image[] decorationImages;

    [Header("Optional")]
    public TowerManager towerManager;

    void Awake()
    {
        if (towerManager == null)
            towerManager = TowerManager.Instance;
    }

    void OnEnable()
    {
        if (IsCarouselActive())
            return;

        TowerManager.OnSelectionChanged += ApplyCurrentTheme;
        ApplyCurrentTheme();
    }

    void OnDisable()
    {
        TowerManager.OnSelectionChanged -= ApplyCurrentTheme;
    }

    static bool IsCarouselActive()
    {
        if (HomeTowerCarouselController.IsSlideControllerActive)
            return true;

        HomeTowerCarouselController carousel = Object.FindObjectOfType<HomeTowerCarouselController>();
        return carousel != null && carousel.isActiveAndEnabled;
    }

    public void ApplyCurrentTheme()
    {
        if (IsCarouselActive())
            return;

        if (towerManager == null)
            towerManager = TowerManager.Instance;

        Tower tower = towerManager != null ? towerManager.GetCurrentTower() : null;
        TowerHomeTheme theme = tower != null ? tower.GetResolvedHomeTheme() : null;

        if (theme == null)
        {
            HomeLayerPlacementUtility.ApplyFullScreenSky(skyImage, null);
            HomeLayerPlacementUtility.ApplyPlacedLayer(towerImage, null, null);
            HomeLayerPlacementUtility.ApplyLayerSlots(groundImages, null);
            HomeLayerPlacementUtility.ApplyLayerSlots(backgroundImages, null);
            HomeLayerPlacementUtility.ApplyLayerSlots(decorationImages, null);
            return;
        }

        HomeLayerPlacementUtility.ApplyFullScreenSky(skyImage, theme.skyBackground);
        HomeLayerPlacementUtility.ApplyPlacedLayer(towerImage, theme.towerForeground, theme.towerPlacement);
        HomeLayerPlacementUtility.ApplyLayerSlots(groundImages, theme.groundLayers);
        HomeLayerPlacementUtility.ApplyLayerSlots(backgroundImages, theme.backgroundLayers);
        HomeLayerPlacementUtility.ApplyLayerSlots(decorationImages, theme.decorationLayers);
    }
}
