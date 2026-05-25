using UnityEngine;

/// <summary>
/// Per-tower home screen visuals. Assign on each entry in <see cref="TowerManager.allTowers"/>.
/// </summary>
[System.Serializable]
public class TowerHomeTheme
{
    [Header("Sky (full screen)")]
    public Sprite skyBackground;

    [Header("Tower (required sprite for visible tower)")]
    [Tooltip("Falls back to legacy Tower.homeTowerImage when empty.")]
    public Sprite towerForeground;
    public HomeLayerPlacement towerPlacement = new HomeLayerPlacement();

    [Header("Ground layers (optional)")]
    [Tooltip("Leave sprite empty to hide that slot for this tower.")]
    public HomePlacedLayerData[] groundLayers;

    [Header("Background layers (optional)")]
    [Tooltip("Leave sprite empty to hide that slot for this tower.")]
    public HomePlacedLayerData[] backgroundLayers;

    [Header("Decoration layers (optional)")]
    [Tooltip("Leave sprite empty to hide that slot for this tower.")]
    public HomePlacedLayerData[] decorationLayers;
}

/// <summary>Static placed image (ground / background / decoration). Null or empty sprite hides the slot.</summary>
[System.Serializable]
public class HomePlacedLayerData
{
    public Sprite sprite;
    public HomeLayerPlacement placement = new HomeLayerPlacement();
}

public static class TowerHomeThemeExtensions
{
    public static Sprite GetTowerForegroundSprite(this Tower tower)
    {
        if (tower == null)
            return null;

        if (tower.homeTheme != null && tower.homeTheme.towerForeground != null)
            return tower.homeTheme.towerForeground;

        return tower.homeTowerImage;
    }

    public static TowerHomeTheme GetResolvedHomeTheme(this Tower tower)
    {
        if (tower == null)
            return null;

        TowerHomeTheme theme = tower.homeTheme;
        if (theme == null)
            theme = new TowerHomeTheme();

        if (theme.towerForeground == null && tower.homeTowerImage != null)
            theme.towerForeground = tower.homeTowerImage;

        return theme;
    }
}
