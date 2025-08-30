using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ParallaxObject
{
    public Transform target;
    public float parallaxFactor = 0.5f;
    [HideInInspector]
    public float spriteHeight = 0f;
    [HideInInspector]
    public Transform duplicate;
}

public class ParallaxController : MonoBehaviour
{
    [Header("Camera Reference")]
    [Tooltip("Camera will be auto-found from GamePersistentScene if not assigned")]
    public Transform cameraTransform;
    
    [Header("Parallax Objects")]
    public List<ParallaxObject> parallaxObjects = new List<ParallaxObject>();

    private Vector3 lastCameraPosition;

    void Start()
    {
        // Use coroutine to ensure camera from persistent scene is found
        StartCoroutine(InitializeAfterCameraFound());
    }
    
    System.Collections.IEnumerator InitializeAfterCameraFound()
    {
        // Wait a moment for GamePersistentScene to load
        yield return new WaitForSeconds(0.1f);
        
        // Auto-find camera if not assigned
        FindCameraReference();
        
        if (cameraTransform == null)
        {
            Debug.LogError("[ParallaxController] Camera not found! Make sure GamePersistentScene is loaded with Main Camera");
            yield break;
        }
        
        lastCameraPosition = cameraTransform.position;
        
        Debug.Log("[ParallaxController] Initialization complete, parallax system ready");

        // Auto-fetch sprite/object height and create duplicates
        foreach (var obj in parallaxObjects)
        {
            if (obj.target == null) continue;
            if (obj.spriteHeight <= 0f)
            {
                SpriteRenderer sr = obj.target.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    obj.spriteHeight = sr.bounds.size.y;
                }
                else
                {
                    Renderer rend = obj.target.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        obj.spriteHeight = rend.bounds.size.y;
                    }
                    else
                    {
                        RectTransform rt = obj.target.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            obj.spriteHeight = rt.rect.height * obj.target.lossyScale.y;
                        }
                    }
                }
            }

            if (obj.spriteHeight > 0)
            {
                obj.duplicate = Instantiate(obj.target, obj.target.parent);
                obj.duplicate.position = obj.target.position + new Vector3(0, obj.spriteHeight, 0);
            }
        }
    }

    void LateUpdate()
    {
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        foreach (var obj in parallaxObjects)
        {
            if (obj.target == null || obj.duplicate == null || obj.spriteHeight <= 0f) continue;

            // Parallax movement
            obj.target.position += new Vector3(0, deltaMovement.y * obj.parallaxFactor, 0);
            obj.duplicate.position += new Vector3(0, deltaMovement.y * obj.parallaxFactor, 0);

            // Infinite scroll in Y
            if (deltaMovement.y > 0) // Moving up
            {
                if (cameraTransform.position.y - obj.target.position.y > obj.spriteHeight)
                {
                    obj.target.position += new Vector3(0, obj.spriteHeight * 2f, 0);
                }
                if (cameraTransform.position.y - obj.duplicate.position.y > obj.spriteHeight)
                {
                    obj.duplicate.position += new Vector3(0, obj.spriteHeight * 2f, 0);
                }
            }
            else // Moving down
            {
                if (obj.target.position.y - cameraTransform.position.y > obj.spriteHeight)
                {
                    obj.target.position -= new Vector3(0, obj.spriteHeight * 2f, 0);
                }
                if (obj.duplicate.position.y - cameraTransform.position.y > obj.spriteHeight)
                {
                    obj.duplicate.position -= new Vector3(0, obj.spriteHeight * 2f, 0);
                }
            }
        }
        lastCameraPosition = cameraTransform.position;
    }
    
    void FindCameraReference()
    {
        // Auto-find camera if not assigned
        if (cameraTransform == null)
        {
            // First try to find Camera.main
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
                Debug.Log("[ParallaxController] Auto-found Main Camera from GamePersistentScene");
            }
            else
            {
                // Fallback: find any camera in the scene
                Camera anyCamera = FindObjectOfType<Camera>();
                if (anyCamera != null)
                {
                    cameraTransform = anyCamera.transform;
                    Debug.Log("[ParallaxController] Auto-found Camera from GamePersistentScene");
                }
                else
                {
                    Debug.LogWarning("[ParallaxController] No camera found! Make sure GamePersistentScene has a Camera");
                }
            }
        }
    }
}