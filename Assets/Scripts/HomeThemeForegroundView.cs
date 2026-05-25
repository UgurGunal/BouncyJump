using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tower art that slides (ground, backgrounds, decorations, tower). Does not include sky or clouds.
/// </summary>
public class HomeThemeForegroundView : MonoBehaviour
{
    public Image towerImage;

    public Image[] groundImages;
    public Image[] backgroundImages;
    public Image[] decorationImages;

    public RectTransform PanelRect => transform as RectTransform;

    public void SetSlideOffset(float x)
    {
        if (PanelRect != null)
            PanelRect.anchoredPosition = new Vector2(x, 0f);
    }

    public void ApplyTheme(TowerHomeTheme theme)
    {
        if (theme == null)
        {
            HomeLayerPlacementUtility.ApplyPlacedLayer(towerImage, null, null);
            HomeLayerPlacementUtility.ApplyLayerSlots(groundImages, null);
            HomeLayerPlacementUtility.ApplyLayerSlots(backgroundImages, null);
            HomeLayerPlacementUtility.ApplyLayerSlots(decorationImages, null);
            return;
        }

        HomeLayerPlacementUtility.ApplyPlacedLayer(towerImage, theme.towerForeground, theme.towerPlacement);
        HomeLayerPlacementUtility.ApplyLayerSlots(groundImages, theme.groundLayers);
        HomeLayerPlacementUtility.ApplyLayerSlots(backgroundImages, theme.backgroundLayers);
        HomeLayerPlacementUtility.ApplyLayerSlots(decorationImages, theme.decorationLayers);
    }

    public void ApplyTower(Tower tower)
    {
        ApplyTheme(tower != null ? tower.GetResolvedHomeTheme() : null);
    }

    void OnValidate()
    {
        DisableRaycasts();
    }

    public void DisableRaycasts()
    {
        HomeLayerPlacementUtility.DisableRaycast(towerImage);
        DisableRaycastArray(groundImages);
        DisableRaycastArray(backgroundImages);
        DisableRaycastArray(decorationImages);
    }

    static void DisableRaycastArray(Image[] images)
    {
        if (images == null)
            return;

        for (int i = 0; i < images.Length; i++)
            HomeLayerPlacementUtility.DisableRaycast(images[i]);
    }
}
