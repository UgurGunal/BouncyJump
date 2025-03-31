using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float upwardSpeed = 0.8f;
    public float ballRelativePosition = 4f;
    public float startCameraOffset = 0f;
    public float cameraMovementStartPos = 20f;

    private float cameraPos; // Track the camera position

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

        // Update balls position while going upwards
        if (target.position.y - ballRelativePosition > cameraPos)
        {
            cameraPos = Mathf.Max(target.position.y - ballRelativePosition, startCameraOffset);
        }

        

        // At the start camera position shouldnt be negative
        if(target.position.y > cameraMovementStartPos)
        {
            cameraPos += upwardSpeed * Time.deltaTime;
        }
        

        transform.position = new Vector3(transform.position.x, cameraPos, transform.position.z);

        //Game end condition
        if (target.position.y < transform.position.y - Camera.main.orthographicSize - 0.5)
        {
            FindObjectOfType<GameManager>().endGame();
        }
    }


}
