using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ParallaxObject
{
    public Transform target;
    public float parallaxFactor = 0.5f;
    [Tooltip("Optional manual tile height (world units). 0 = auto from sprite.")]
    public float manualTileHeight;
    [HideInInspector] public float tileHeight;
    [HideInInspector] public Transform duplicate;
    [HideInInspector] public float baseZ;
    [HideInInspector] public int baseSortingOrder;
}

/// <summary>
/// Infinite vertical parallax backgrounds using two stacked tiles.
/// Tiles are overlapped and inset, and the upper tile always wins the 2D sort
/// so the joint never flashes a bright atlas edge or a gap behind the art.
/// </summary>
[DefaultExecutionOrder(100)]
public class ParallaxController : MonoBehaviour
{
    [Header("Camera Reference")]
    [Tooltip("Camera will be auto-found from GamePersistentScene if not assigned")]
    public Transform cameraTransform;

    [Header("Parallax Objects")]
    public List<ParallaxObject> parallaxObjects = new List<ParallaxObject>();

    [Header("Seam Fix")]
    [Tooltip("How far the two tiles slide into each other (world units). Larger = harder to see the joint.")]
    public float seamOverlapWorld = 0.03f;
    [Tooltip("Extra overlap in screen pixels (scales with resolution / ortho size).")]
    public int seamOverlapPixels = 2;
    [Tooltip("Texture pixels trimmed off the top and bottom of each tile sprite. Atlas margins are opaque white and bleed through filtering / compression.")]
    [Range(0, 16)] public int spriteEdgeInsetPixels = 4;
    [Tooltip("Depth gap between tiles (only used if transparency sort mode is Custom Axis / Z). Prefer sortingOrder for Default mode.")]
    public float tileDepthSeparation = 0.01f;

    Vector3 lastCameraPosition;
    float worldPixelSize = -1f;
    readonly List<Sprite> generatedSprites = new List<Sprite>();

    void Start()
    {
        StartCoroutine(InitializeAfterCameraFound());
    }

    IEnumerator InitializeAfterCameraFound()
    {
        yield return new WaitForSeconds(0.1f);
        FindCameraReference();

        if (cameraTransform == null)
            yield break;

        lastCameraPosition = cameraTransform.position;
        RefreshWorldPixelSize();

        foreach (ParallaxObject obj in parallaxObjects)
            SetupParallaxObject(obj);
    }

    void LateUpdate()
    {
        if (cameraTransform == null)
            return;

        RefreshWorldPixelSize();

        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;

        foreach (ParallaxObject obj in parallaxObjects)
            UpdateParallaxObject(obj, deltaMovement);

        lastCameraPosition = cameraTransform.position;
    }

    void OnDestroy()
    {
        foreach (Sprite sprite in generatedSprites)
        {
            if (sprite != null)
                Destroy(sprite);
        }

        generatedSprites.Clear();
    }

    void SetupParallaxObject(ParallaxObject obj)
    {
        if (obj.target == null)
            return;

        if (obj.duplicate != null)
            Destroy(obj.duplicate.gameObject);

        ApplyEdgeInset(obj.target);

        obj.tileHeight = obj.manualTileHeight > 0f ? obj.manualTileHeight : MeasureTileHeight(obj.target);
        if (obj.tileHeight <= 0f)
            return;

        obj.baseZ = obj.target.position.z;

        SpriteRenderer sourceRenderer = obj.target.GetComponent<SpriteRenderer>();
        obj.baseSortingOrder = sourceRenderer != null ? sourceRenderer.sortingOrder : 0;

        obj.duplicate = Instantiate(obj.target, obj.target.parent);
        obj.duplicate.name = obj.target.name + "_ParallaxDuplicate";

        float step = GetTileStep(obj.tileHeight);
        obj.duplicate.position = obj.target.position + Vector3.up * step;

        CopySpriteSettings(obj.target, obj.duplicate);
        ApplySeamSort(obj.target, obj.duplicate, obj);
    }

    void UpdateParallaxObject(ParallaxObject obj, Vector3 deltaMovement)
    {
        if (obj.target == null || obj.duplicate == null || obj.tileHeight <= 0f)
            return;

        float step = GetTileStep(obj.tileHeight);
        float targetY = obj.target.position.y;
        float duplicateY = obj.duplicate.position.y;

        Transform below = targetY <= duplicateY ? obj.target : obj.duplicate;
        Transform above = below == obj.target ? obj.duplicate : obj.target;

        float belowY = Mathf.Min(targetY, duplicateY) + deltaMovement.y * obj.parallaxFactor;
        float cameraY = cameraTransform.position.y;

        if (deltaMovement.y > 0f && cameraY - belowY > step)
        {
            belowY += step;
            Transform wrapped = below;
            below = above;
            above = wrapped;
        }
        else if (deltaMovement.y < 0f && belowY > cameraY)
        {
            belowY -= step;
            Transform wrapped = above;
            above = below;
            below = wrapped;
        }

        // Keep the pair exactly one step apart so a sub-pixel gap never opens.
        // Upper tile stays in front via sortingOrder (Default 2D sort ignores Z).
        above.position = new Vector3(above.position.x, belowY + step, obj.baseZ);
        below.position = new Vector3(below.position.x, belowY, obj.baseZ + tileDepthSeparation);
        ApplySeamSort(above, below, obj);
    }

    /// <summary>
    /// Default transparency sort ignores Z, so both tiles must differ in sortingOrder.
    /// Lower tile is pushed one order behind the original so nothing jumps in front of gameplay.
    /// </summary>
    static void ApplySeamSort(Transform above, Transform below, ParallaxObject obj)
    {
        SpriteRenderer aboveRenderer = above.GetComponent<SpriteRenderer>();
        SpriteRenderer belowRenderer = below.GetComponent<SpriteRenderer>();

        if (aboveRenderer != null)
            aboveRenderer.sortingOrder = obj.baseSortingOrder;
        if (belowRenderer != null)
            belowRenderer.sortingOrder = obj.baseSortingOrder - 1;
    }

    /// <summary>
    /// Rebuilds the tile sprite from a slightly smaller rect so the outermost texture rows,
    /// which pick up the white atlas margin through filtering and block compression, are
    /// never sampled.
    /// </summary>
    void ApplyEdgeInset(Transform tile)
    {
        if (spriteEdgeInsetPixels <= 0)
            return;

        SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
            return;

        Sprite source = renderer.sprite;
        if (source.texture == null || generatedSprites.Contains(source))
            return;

        Rect rect = source.rect;
        float inset = Mathf.Min(spriteEdgeInsetPixels, (rect.height - 1f) * 0.5f);
        if (inset <= 0f)
            return;

        Rect inner = new Rect(rect.x, rect.y + inset, rect.width, rect.height - inset * 2f);
        Vector2 pivot = new Vector2(
            Mathf.Clamp01(source.pivot.x / rect.width),
            Mathf.Clamp01((source.pivot.y - inset) / inner.height));

        Sprite trimmed = Sprite.Create(source.texture, inner, pivot, source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        trimmed.name = source.name + "_EdgeInset";

        renderer.sprite = trimmed;
        generatedSprites.Add(trimmed);
    }

    float GetTileStep(float tileHeight)
    {
        float pixelOverlap = seamOverlapPixels * Mathf.Max(worldPixelSize, 0f);
        float overlap = seamOverlapWorld + pixelOverlap;
        return Mathf.Max(0.01f, tileHeight - overlap);
    }

    void RefreshWorldPixelSize()
    {
        if (cameraTransform == null)
            return;

        Camera cam = cameraTransform.GetComponent<Camera>();
        if (cam != null && cam.orthographic)
            worldPixelSize = (2f * cam.orthographicSize) / Mathf.Max(1, cam.pixelHeight);
    }

    static float MeasureTileHeight(Transform target)
    {
        SpriteRenderer spriteRenderer = target.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            float scaleY = target.lossyScale.y;
            return spriteRenderer.sprite.rect.height / spriteRenderer.sprite.pixelsPerUnit * scaleY;
        }

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
            return renderer.bounds.size.y;

        RectTransform rectTransform = target.GetComponent<RectTransform>();
        if (rectTransform != null)
            return rectTransform.rect.height * target.lossyScale.y;

        return 0f;
    }

    static void CopySpriteSettings(Transform source, Transform duplicate)
    {
        SpriteRenderer sourceRenderer = source.GetComponent<SpriteRenderer>();
        SpriteRenderer duplicateRenderer = duplicate.GetComponent<SpriteRenderer>();
        if (sourceRenderer == null || duplicateRenderer == null)
            return;

        duplicateRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        duplicateRenderer.sortingOrder = sourceRenderer.sortingOrder;
    }

    void FindCameraReference()
    {
        if (cameraTransform != null)
            return;

        if (Camera.main != null)
            cameraTransform = Camera.main.transform;
        else
        {
            Camera anyCamera = FindObjectOfType<Camera>();
            if (anyCamera != null)
                cameraTransform = anyCamera.transform;
        }
    }
}
