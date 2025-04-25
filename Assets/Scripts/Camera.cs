using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float ballRelativePosition = 4f;
    [SerializeField] private float startCameraOffset = 0f;
    [SerializeField] private float cameraMovementStartPos = 20f;
    [SerializeField] private float upperBorder = 1f; // Camera follows when ball passes camera.y + 1


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
        if (target.position.y - ballRelativePosition - upperBorder> cameraPos)
        {
            cameraPos = Mathf.Max(target.position.y - ballRelativePosition - upperBorder, startCameraOffset);
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
