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
}

/// <summary>
/// Infinite vertical parallax backgrounds using two stacked tiles.
/// Seam overlap + pixel snapping reduce white lines between tiles.
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
    [Tooltip("Extra overlap between stacked background pieces (world units).")]
    public float seamOverlapWorld = 0.02f;
    [Tooltip("Extra overlap in screen pixels (good for pixel-art sprites).")]
    public int seamOverlapPixels = 2;
    [Tooltip("Snap tile Y positions to the camera pixel grid.")]
    public bool snapTilesToPixelGrid = true;

    Vector3 lastCameraPosition;
    float worldPixelSize = -1f;

    void Start()
    {
        StartCoroutine(InitializeAfterCameraFound());
    }

    IEnumerator InitializeAfterCameraFound()
    {
        yield return new WaitForSeconds(0.1f);
        FindCameraReference();

        if (cameraTransform == null)
        {
            yield break;
        }

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

    void SetupParallaxObject(ParallaxObject obj)
    {
        if (obj.target == null)
            return;

        obj.tileHeight = obj.manualTileHeight > 0f ? obj.manualTileHeight : MeasureTileHeight(obj.target);
        if (obj.tileHeight <= 0f)
            return;

        if (obj.duplicate != null)
            Destroy(obj.duplicate.gameObject);

        obj.duplicate = Instantiate(obj.target, obj.target.parent);
        obj.duplicate.name = obj.target.name + "_ParallaxDuplicate";

        float step = GetTileStep(obj.tileHeight);

        Vector3 basePos = obj.target.position;
        basePos.y = SnapY(basePos.y);
        obj.target.position = basePos;

        Vector3 duplicatePos = basePos + Vector3.up * step;
        duplicatePos.y = SnapY(duplicatePos.y);
        obj.duplicate.position = duplicatePos;

        CopySpriteSettings(obj.target, obj.duplicate);
    }

    void UpdateParallaxObject(ParallaxObject obj, Vector3 deltaMovement)
    {
        if (obj.target == null || obj.duplicate == null || obj.tileHeight <= 0f)
            return;

        float step = GetTileStep(obj.tileHeight);
        Vector3 parallaxDelta = new Vector3(0f, deltaMovement.y * obj.parallaxFactor, 0f);

        obj.target.position += parallaxDelta;
        obj.duplicate.position += parallaxDelta;

        float cameraY = cameraTransform.position.y;
        Transform lower = obj.target.position.y <= obj.duplicate.position.y ? obj.target : obj.duplicate;
        Transform upper = lower == obj.target ? obj.duplicate : obj.target;

        if (deltaMovement.y > 0f)
        {
            if (cameraY - lower.position.y > step)
            {
                float newY = SnapY(upper.position.y + step);
                lower.position = new Vector3(lower.position.x, newY, lower.position.z);
            }
        }
        else if (deltaMovement.y < 0f)
        {
            if (upper.position.y - cameraY > step)
            {
                float newY = SnapY(lower.position.y - step);
                upper.position = new Vector3(upper.position.x, newY, upper.position.z);
            }
        }
    }

    float GetTileStep(float tileHeight)
    {
        float pixelOverlap = seamOverlapPixels * Mathf.Max(worldPixelSize, 0f);
        float overlap = seamOverlapWorld + pixelOverlap;
        return Mathf.Max(0.01f, tileHeight - overlap);
    }

    float SnapY(float y)
    {
        if (!snapTilesToPixelGrid || worldPixelSize <= 0f)
            return y;

        return Mathf.Round(y / worldPixelSize) * worldPixelSize;
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
