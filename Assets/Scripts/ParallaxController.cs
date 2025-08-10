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
    public Transform cameraTransform;
    public List<ParallaxObject> parallaxObjects = new List<ParallaxObject>();

    private Vector3 lastCameraPosition;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
        lastCameraPosition = cameraTransform.position;

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
}