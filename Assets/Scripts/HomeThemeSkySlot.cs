using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen sky for one tower slot. Slides with <see cref="HomeThemeForegroundView"/>; clouds stay separate.
/// </summary>
public class HomeThemeSkySlot : MonoBehaviour
{
    public Image skyImage;

    public RectTransform SkyRect =>
        skyImage != null ? skyImage.rectTransform : transform as RectTransform;

    public void ApplyTheme(TowerHomeTheme theme)
    {
        HomeLayerPlacementUtility.ApplyFullScreenSky(skyImage, theme != null ? theme.skyBackground : null);
    }

    public void ApplyTower(Tower tower)
    {
        ApplyTheme(tower != null ? tower.GetResolvedHomeTheme() : null);
    }

    public void SetSlideOffset(float x)
    {
        RectTransform rect = transform as RectTransform;
        if (rect != null)
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
        else if (SkyRect != null)
            SkyRect.anchoredPosition = new Vector2(x, SkyRect.anchoredPosition.y);
    }

    void OnValidate()
    {
        if (skyImage == null)
        {
            skyImage = GetComponent<Image>();
            if (skyImage == null)
                skyImage = GetComponentInChildren<Image>(true);
        }

        HomeLayerPlacementUtility.DisableRaycast(skyImage);
    }
}
