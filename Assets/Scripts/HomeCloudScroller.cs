using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Add to a cloud UI object that already has an <see cref="Image"/> (sprite, size, position set on that object).
/// Only configures scroll; does not override sprite, width, height, or position.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class HomeCloudScroller : MonoBehaviour
{
    [Header("Scroll")]
    public float speed = 25f;
    public Vector2 direction = Vector2.right;
    public bool scroll = true;

    [Header("Horizontal teleport")]
    [Tooltip("When moving right and X reaches this value, teleport to Wrap To X.")]
    public float wrapFromX = 1000f;
    [Tooltip("Teleport destination X (e.g. -1000).")]
    public float wrapToX = -1000f;

    Image targetImage;
    RectTransform rectTransform;
    Vector2 scrollDirection = Vector2.right;
    float scrollSpeed;
    bool scrolling;
    bool initialized;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        targetImage = GetComponent<Image>();
        InitializeScroll();
        initialized = true;
    }

    void OnEnable()
    {
        if (!initialized)
        {
            InitializeScroll();
            initialized = true;
            return;
        }

        RefreshScrollState();
    }

    void InitializeScroll()
    {
        bool hasSprite = targetImage != null && targetImage.sprite != null;

        if (targetImage != null)
        {
            targetImage.preserveAspect = true;
            targetImage.enabled = hasSprite;
            targetImage.raycastTarget = false;
        }

        RefreshScrollState();
    }

    void RefreshScrollState()
    {
        bool hasSprite = targetImage != null && targetImage.sprite != null;
        scrollDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        scrollSpeed = Mathf.Max(0f, speed);
        scrolling = hasSprite && scroll && scrollSpeed > 0f;
    }

    void Update()
    {
        if (!scrolling || rectTransform == null)
            return;

        Vector2 pos = rectTransform.anchoredPosition;
        pos += scrollDirection * (scrollSpeed * Time.deltaTime);
        ApplyHorizontalTeleport(ref pos);
        rectTransform.anchoredPosition = pos;
    }

    void ApplyHorizontalTeleport(ref Vector2 pos)
    {
        if (Mathf.Abs(scrollDirection.x) < 0.01f)
            return;

        if (scrollDirection.x > 0f)
        {
            if (pos.x >= wrapFromX)
                pos.x = wrapToX;
        }
        else if (pos.x <= wrapToX)
        {
            pos.x = wrapFromX;
        }
    }
}
