using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraScaler : MonoBehaviour
{
    public float targetHeight = 22f; // World height you want to show (e.g., 16 units)

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        ScaleCamera();
    }

#if UNITY_EDITOR
    void Update() // For testing in the editor
    {
        //ScaleCamera();
    }
#endif

    void ScaleCamera()
    {
        if (cam == null) cam = GetComponent<Camera>();

        // Set orthographic size based on target world height
        float orthoSize = targetHeight * 0.5f;
        cam.orthographicSize = orthoSize;
    }
}
