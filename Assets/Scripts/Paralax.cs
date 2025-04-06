using UnityEngine;
using System.Collections.Generic;

// This script manages multiple background layers
public class ParallaxBackgroundManager : MonoBehaviour
{
    [System.Serializable]
    public class BackgroundLayer
    {
        public GameObject backgroundTemplate;
        public float parallaxSpeedY = 0.5f;
    }

    [Header("Background Layers")]
    [SerializeField] private List<BackgroundLayer> backgroundLayers = new List<BackgroundLayer>();

    void Start()
    {
        // Create a separate controller for each layer
        foreach (BackgroundLayer layer in backgroundLayers)
        {
            if (layer.backgroundTemplate == null)
                continue;

            // Create a container for this layer
            GameObject layerObj = new GameObject(layer.backgroundTemplate.name + "_Layer");
            layerObj.transform.SetParent(transform);

            // Add a standard ParallaxBackground component
            ParallaxBackground controller = layerObj.AddComponent<ParallaxBackground>();

            // Configure it
            controller.Setup(layer.backgroundTemplate, layer.parallaxSpeedY);
        }
    }
}

// Modified original script with a public Setup method
public class ParallaxBackground : MonoBehaviour
{
    private GameObject backgroundTemplate;
    private float parallaxSpeedY = 0.5f;

    private GameObject[] backgroundInstances = new GameObject[2];
    private float backgroundHeight;
    private Camera mainCamera;
    private Vector3 lastCameraPosition;

    // Public setup method to configure this component after creation
    public void Setup(GameObject template, float speed)
    {
        backgroundTemplate = template;
        parallaxSpeedY = speed;

        // Call Start manually since we're setting up after Awake/Start
        Initialize();
    }

    void Start()
    {
        // Only run automatic initialization if we haven't been configured by Setup()
        if (backgroundTemplate == null)
        {
            // This allows the component to still be used directly with inspector values
            backgroundTemplate = GetComponent<SpriteRenderer>()?.gameObject;
            if (backgroundTemplate == null)
            {
                Debug.LogWarning("ParallaxBackground: No background template assigned");
                return;
            }
            Initialize();
        }
    }

    private void Initialize()
    {
        mainCamera = Camera.main;
        lastCameraPosition = mainCamera.transform.position;

        // Set up backgrounds using the provided GameObject
        if (backgroundTemplate != null)
        {
            // Get height from the sprite renderer
            SpriteRenderer spriteRenderer = backgroundTemplate.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                backgroundHeight = spriteRenderer.bounds.size.y;
            }
            else
            {
                Debug.LogError("Background object must have a SpriteRenderer component");
                return;
            }

            // Create first background (clone the original)
            backgroundInstances[0] = Instantiate(backgroundTemplate, transform);
            backgroundInstances[0].name = "Background_0";
            backgroundInstances[0].SetActive(true);

            // Create second background (another clone)
            backgroundInstances[1] = Instantiate(backgroundTemplate, transform);
            backgroundInstances[1].name = "Background_1";
            backgroundInstances[1].SetActive(true);

            // Position the second background above the first one
            backgroundInstances[1].transform.position = backgroundInstances[0].transform.position + new Vector3(0, backgroundHeight, 0);

            // Hide the template - we don't need to see it anymore
            backgroundTemplate.SetActive(false);
        }
    }

    void Update()
    {
        if (mainCamera == null || backgroundInstances[0] == null)
            return;

        // Calculate camera movement since last frame
        Vector3 cameraMovement = mainCamera.transform.position - lastCameraPosition;

        // Only move backgrounds if the camera has moved
        if (cameraMovement.magnitude > 0.001f)
        {
            // Calculate parallax effect based on camera movement in Y direction
            float parallaxEffect = cameraMovement.y * parallaxSpeedY;

            // Apply parallax to both backgrounds
            foreach (GameObject bg in backgroundInstances)
            {
                if (bg != null)
                    bg.transform.position += new Vector3(0, parallaxEffect, 0);
            }

            // Check if we need to reposition backgrounds
            RepositionBackgrounds();
        }

        // Update last camera position
        lastCameraPosition = mainCamera.transform.position;
    }

    void RepositionBackgrounds()
    {
        // Find the lowest background (will be the one to reposition)
        int lowestIndex = 0;
        float lowestY = backgroundInstances[0].transform.position.y;

        for (int i = 1; i < backgroundInstances.Length; i++)
        {
            if (backgroundInstances[i].transform.position.y < lowestY)
            {
                lowestY = backgroundInstances[i].transform.position.y;
                lowestIndex = i;
            }
        }

        // Find the highest background
        int highestIndex = (lowestIndex == 0) ? 1 : 0;
        float highestY = backgroundInstances[highestIndex].transform.position.y;

        // If the lowest background has moved below the camera view, reposition it
        float cameraBottomY = mainCamera.transform.position.y - (mainCamera.orthographicSize);

        if (lowestY + backgroundHeight < cameraBottomY)
        {
            // Move the lowest background to be above the highest one
            backgroundInstances[lowestIndex].transform.position = new Vector3(
                backgroundInstances[lowestIndex].transform.position.x,
                highestY + backgroundHeight,
                backgroundInstances[lowestIndex].transform.position.z
            );
        }

        // If the highest background has moved below the camera view at the top, reposition the lowest one
        float cameraTopY = mainCamera.transform.position.y + (mainCamera.orthographicSize);

        if (highestY > cameraTopY + backgroundHeight)
        {
            // Move the highest background to be below the lowest one
            backgroundInstances[highestIndex].transform.position = new Vector3(
                backgroundInstances[highestIndex].transform.position.x,
                lowestY - backgroundHeight,
                backgroundInstances[highestIndex].transform.position.z
            );
        }
    }
}