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
    [HideInInspector] public SpriteRenderer targetRenderer;
    [HideInInspector] public SpriteRenderer duplicateRenderer;
    [HideInInspector] public Transform lastAbove;
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
    float cachedOrthoSize = -1f;
    int cachedPixelHeight = -1;
    Camera cachedCamera;
    readonly List<Sprite> generatedSprites = new List<Sprite>();
    readonly Dictionary<int, Sprite> trimmedSpriteCache = new Dictionary<int, Sprite>();

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
        RefreshWorldPixelSize(force: true);

        foreach (ParallaxObject obj in parallaxObjects)
            SetupParallaxObject(obj);
    }

    void LateUpdate()
    {
        if (cameraTransform == null)
            return;

        RefreshWorldPixelSize(force: false);

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
        trimmedSpriteCache.Clear();
    }

    void SetupParallaxObject(ParallaxObject obj)
    {
        if (obj.target == null)
            return;

        if (obj.duplicate != null)
            Destroy(obj.duplicate.gameObject);

        obj.targetRenderer = obj.target.GetComponent<SpriteRenderer>();
        ApplyEdgeInset(obj.targetRenderer);

        obj.tileHeight = obj.manualTileHeight > 0f ? obj.manualTileHeight : MeasureTileHeight(obj.target, obj.targetRenderer);
        if (obj.tileHeight <= 0f)
            return;

        obj.baseZ = obj.target.position.z;
        obj.baseSortingOrder = obj.targetRenderer != null ? obj.targetRenderer.sortingOrder : 0;

        obj.duplicate = Instantiate(obj.target, obj.target.parent);
        obj.duplicate.name = obj.target.name + "_ParallaxDuplicate";
        obj.duplicateRenderer = obj.duplicate.GetComponent<SpriteRenderer>();

        float step = GetTileStep(obj.tileHeight);
        obj.duplicate.position = obj.target.position + Vector3.up * step;

        CopySpriteSettings(obj.targetRenderer, obj.duplicateRenderer);
        obj.lastAbove = null;
        ApplySeamSort(obj, obj.duplicate, obj.target);
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
        bool wrapped = false;

        if (deltaMovement.y > 0f && cameraY - belowY > step)
        {
            belowY += step;
            Transform wrappedTile = below;
            below = above;
            above = wrappedTile;
            wrapped = true;
        }
        else if (deltaMovement.y < 0f && belowY > cameraY)
        {
            belowY -= step;
            Transform wrappedTile = above;
            above = below;
            below = wrappedTile;
            wrapped = true;
        }

        // Keep the pair exactly one step apart so a sub-pixel gap never opens.
        // Upper tile stays in front via sortingOrder (Default 2D sort ignores Z).
        above.position = new Vector3(above.position.x, belowY + step, obj.baseZ);
        below.position = new Vector3(below.position.x, belowY, obj.baseZ + tileDepthSeparation);

        // Only rewrite sortingOrder when the above/below roles change (wrap) or on first update.
        if (wrapped || obj.lastAbove != above)
            ApplySeamSort(obj, above, below);
    }

    /// <summary>
    /// Default transparency sort ignores Z, so both tiles must differ in sortingOrder.
    /// Lower tile is pushed one order behind the original so nothing jumps in front of gameplay.
    /// </summary>
    static void ApplySeamSort(ParallaxObject obj, Transform above, Transform below)
    {
        SpriteRenderer aboveRenderer = above == obj.target ? obj.targetRenderer : obj.duplicateRenderer;
        SpriteRenderer belowRenderer = below == obj.target ? obj.targetRenderer : obj.duplicateRenderer;

        if (aboveRenderer != null)
            aboveRenderer.sortingOrder = obj.baseSortingOrder;
        if (belowRenderer != null)
            belowRenderer.sortingOrder = obj.baseSortingOrder - 1;

        obj.lastAbove = above;
    }

    /// <summary>
    /// Rebuilds the tile sprite from a slightly smaller rect so the outermost texture rows,
    /// which pick up the white atlas margin through filtering and block compression, are
    /// never sampled. Trimmed sprites are cached per source sprite so duplicates share one.
    /// </summary>
    void ApplyEdgeInset(SpriteRenderer renderer)
    {
        if (spriteEdgeInsetPixels <= 0 || renderer == null || renderer.sprite == null)
            return;

        Sprite source = renderer.sprite;
        if (source.texture == null)
            return;

        int sourceId = source.GetInstanceID();
        if (trimmedSpriteCache.TryGetValue(sourceId, out Sprite cached) && cached != null)
        {
            renderer.sprite = cached;
            return;
        }

        // Already a generated inset sprite (e.g. re-init).
        if (generatedSprites.Contains(source))
        {
            trimmedSpriteCache[sourceId] = source;
            return;
        }

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
        trimmedSpriteCache[sourceId] = trimmed;
        trimmedSpriteCache[trimmed.GetInstanceID()] = trimmed;
    }

    float GetTileStep(float tileHeight)
    {
        float pixelOverlap = seamOverlapPixels * Mathf.Max(worldPixelSize, 0f);
        float overlap = seamOverlapWorld + pixelOverlap;
        return Mathf.Max(0.01f, tileHeight - overlap);
    }

    void RefreshWorldPixelSize(bool force)
    {
        if (cameraTransform == null)
            return;

        if (cachedCamera == null)
            cachedCamera = cameraTransform.GetComponent<Camera>();

        if (cachedCamera == null || !cachedCamera.orthographic)
            return;

        int pixelHeight = cachedCamera.pixelHeight;
        float orthoSize = cachedCamera.orthographicSize;
        if (!force && pixelHeight == cachedPixelHeight && Mathf.Approximately(orthoSize, cachedOrthoSize))
            return;

        cachedPixelHeight = pixelHeight;
        cachedOrthoSize = orthoSize;
        worldPixelSize = (2f * orthoSize) / Mathf.Max(1, pixelHeight);
    }

    static float MeasureTileHeight(Transform target, SpriteRenderer spriteRenderer)
    {
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

    static void CopySpriteSettings(SpriteRenderer sourceRenderer, SpriteRenderer duplicateRenderer)
    {
        if (sourceRenderer == null || duplicateRenderer == null)
            return;

        duplicateRenderer.sprite = sourceRenderer.sprite;
        duplicateRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        duplicateRenderer.sortingOrder = sourceRenderer.sortingOrder;
    }

    void FindCameraReference()
    {
        if (cameraTransform != null)
        {
            cachedCamera = cameraTransform.GetComponent<Camera>();
            return;
        }

        if (Camera.main != null)
            cameraTransform = Camera.main.transform;
        else
        {
            Camera anyCamera = FindObjectOfType<Camera>();
            if (anyCamera != null)
                cameraTransform = anyCamera.transform;
        }

        if (cameraTransform != null)
            cachedCamera = cameraTransform.GetComponent<Camera>();
    }
}
