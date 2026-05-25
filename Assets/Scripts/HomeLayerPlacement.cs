using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple X/Y position for a home screen layer (anchored to bottom-center of parent).
/// Set per tower in <see cref="TowerHomeTheme"/>.
/// </summary>
[System.Serializable]
public class HomeLayerPlacement
{
    public float x;
    public float y;

    public const float DefaultAnchorX = 0.5f;
    public const float DefaultAnchorY = 0f;
    public const float DefaultPivotX = 0.5f;
    public const float DefaultPivotY = 0f;

    public Vector2 AnchoredPosition => new Vector2(x, y);
}

public static class HomeLayerPlacementUtility
{
    /// <summary>Home visuals must not block shop/UI button clicks.</summary>
    public static void DisableRaycast(Image image)
    {
        if (image != null)
            image.raycastTarget = false;
    }

    public static void ApplyTo(RectTransform rectTransform, HomeLayerPlacement placement)
    {
        if (rectTransform == null || placement == null)
            return;

        Vector2 anchor = new Vector2(HomeLayerPlacement.DefaultAnchorX, HomeLayerPlacement.DefaultAnchorY);
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(HomeLayerPlacement.DefaultPivotX, HomeLayerPlacement.DefaultPivotY);
        rectTransform.anchoredPosition = placement.AnchoredPosition;
    }

    public static void ApplyNativeSpriteSize(Image image)
    {
        if (image == null || image.sprite == null)
            return;

        image.SetNativeSize();
    }

    /// <summary>Fits sprite inside width x height box while preserving aspect ratio.</summary>
    public static Vector2 GetSizePreservingAspect(Sprite sprite, float boxWidth, float boxHeight)
    {
        if (sprite == null || sprite.rect.height <= 0f)
            return new Vector2(boxWidth, boxHeight);

        float aspect = sprite.rect.width / sprite.rect.height;
        float w = Mathf.Max(0f, boxWidth);
        float h = Mathf.Max(0f, boxHeight);

        if (w <= 0.001f && h <= 0.001f)
            return Vector2.zero;

        if (w <= 0.001f)
            return new Vector2(h * aspect, h);

        if (h <= 0.001f)
            return new Vector2(w, w / aspect);

        if (w / h > aspect)
            w = h * aspect;
        else
            h = w / aspect;

        return new Vector2(w, h);
    }

    public static void ApplyPlacedLayerWithSize(Image image, Sprite sprite, HomeLayerPlacement placement, float width, float height)
    {
        if (image == null)
            return;

        if (sprite == null)
        {
            image.enabled = false;
            image.gameObject.SetActive(false);
            return;
        }

        image.gameObject.SetActive(true);

        RectTransform rectTransform = image.rectTransform;
        if (placement != null)
            ApplyTo(rectTransform, placement);

        image.sprite = sprite;
        image.enabled = true;
        image.preserveAspect = true;
        rectTransform.sizeDelta = GetSizePreservingAspect(sprite, width, height);
        DisableRaycast(image);
    }

    public static void StretchFullScreen(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    public static void ApplyFullScreenSky(Image sky, Sprite sprite)
    {
        if (sky == null)
            return;

        StretchFullScreen(sky.rectTransform);
        sky.sprite = sprite;
        sky.enabled = sprite != null;
        sky.preserveAspect = false;
        sky.gameObject.SetActive(sprite != null);
        DisableRaycast(sky);
    }

    public static void ApplyPlacedLayer(Image image, Sprite sprite, HomeLayerPlacement placement)
    {
        if (image == null)
            return;

        if (sprite == null)
        {
            image.enabled = false;
            image.gameObject.SetActive(false);
            return;
        }

        image.gameObject.SetActive(true);

        RectTransform rectTransform = image.rectTransform;
        if (placement != null)
            ApplyTo(rectTransform, placement);

        image.sprite = sprite;
        image.enabled = true;
        image.preserveAspect = true;
        ApplyNativeSpriteSize(image);
        DisableRaycast(image);
    }

    public static void ApplyPlacedLayer(Image image, HomePlacedLayerData layer)
    {
        if (layer == null)
        {
            ApplyPlacedLayer(image, null, null);
            return;
        }

        ApplyPlacedLayer(image, layer.sprite, layer.placement);
    }

    public static void ApplyLayerSlots(Image[] slots, HomePlacedLayerData[] layers)
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            Image slot = slots[i];
            if (slot == null)
                continue;

            HomePlacedLayerData data = layers != null && i < layers.Length ? layers[i] : null;
            ApplyPlacedLayer(slot, data);
        }
    }
}
