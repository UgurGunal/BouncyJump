using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float ballRelativePosition = 4f;
    public float startCameraOffset = 0f;
    public float cameraMovementStartPos = 20f;

    private float cameraPos; // Track the camera position
    private float upwardCameraSpeed; // This will be set by LevelManager

    void Start()
    {
        if (target != null)
        {
            cameraPos = startCameraOffset;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Update target position while going upwards
        if (target.position.y - ballRelativePosition > cameraPos)
        {
            cameraPos = Mathf.Max(target.position.y - ballRelativePosition, startCameraOffset);
        }

        // Move the camera only after a certain height
        if (target.position.y > cameraMovementStartPos)
        {
            cameraPos += upwardCameraSpeed * Time.deltaTime;
        }

        transform.position = new Vector3(transform.position.x, cameraPos, transform.position.z);

        // Game end condition
        if (target.position.y < transform.position.y - Camera.main.orthographicSize - 0.5)
        {
            FindObjectOfType<GameManager>().endGame();
        }
    }

    // Called by LevelManager when level changes
    public void SetCameraSpeed(float speed)
    {
        upwardCameraSpeed = speed;
    }
}
