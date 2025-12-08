using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Platform))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Moving Platform Settings")]
    [Tooltip("Chance for this platform instance to become a moving platform (0 = never, 1 = always).")]
    [Range(0f, 1f)]
    public float movingPlatformSpawnRate = 0.1f;

    [Tooltip("Horizontal speed of the platform when it is a moving platform.")]
    public float moveSpeed = 2f;

    private const float directionLeft = -1f;
    private const float directionRight = 1f;
    private const float minXPosition = -2f;
    private const float maxXPosition = 2f;

    private Platform platformComponent;
    private Rigidbody2D cachedRigidbody;

    private bool hasDecided;
    private bool isMovingPlatform;
    private float currentDirection;

    private static FieldInfo originalPositionField;

    /// <summary>
    /// Returns true if this instance has been initialized as a moving platform.
    /// </summary>
    public bool IsMovingPlatform => isMovingPlatform;

    private void Awake()
    {
        platformComponent = GetComponent<Platform>();
        cachedRigidbody = GetComponent<Rigidbody2D>();

        if (originalPositionField == null)
        {
            originalPositionField = typeof(Platform).GetField("originalPosition", BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }

    private void Start()
    {
        DecidePlatformType();
    }

    private void FixedUpdate()
    {
        if (!isMovingPlatform || moveSpeed <= 0f || Mathf.Approximately(currentDirection, 0f))
        {
            return;
        }

        Vector3 position = transform.position;

        if (position.x <= minXPosition)
        {
            currentDirection = directionRight;
        }
        else if (position.x >= maxXPosition)
        {
            currentDirection = directionLeft;
        }

        MovePlatform(Time.fixedDeltaTime);
    }

    private void DecidePlatformType()
    {
        if (hasDecided)
        {
            return;
        }

        hasDecided = true;

        float clampedRate = Mathf.Clamp01(movingPlatformSpawnRate);
        isMovingPlatform = Random.value < clampedRate;

        if (isMovingPlatform)
        {
            currentDirection = GetInitialDirection();
        }
        else
        {
            currentDirection = 0f;
        }
    }

    private float GetInitialDirection()
    {
        return Random.value < 0.5f ? directionLeft : directionRight;
    }

    private void MovePlatform(float deltaTime)
    {
        Vector3 currentPosition = transform.position;
        currentPosition.x += currentDirection * moveSpeed * deltaTime;

        if (cachedRigidbody != null && cachedRigidbody.bodyType != RigidbodyType2D.Static && cachedRigidbody.simulated)
        {
            cachedRigidbody.MovePosition(currentPosition);
        }
        else
        {
            transform.position = currentPosition;
        }

        SyncOriginalPositionWithPlatform(currentPosition);
    }

    private void SyncOriginalPositionWithPlatform(Vector3 value)
    {
        if (platformComponent != null && originalPositionField != null)
        {
            originalPositionField.SetValue(platformComponent, value);
        }
    }

    /// <summary>
    /// Allows external callers (e.g., spawners) to force this platform to become moving.
    /// </summary>
    public void ForceSetMovingState(bool shouldMove, int direction = 0)
    {
        isMovingPlatform = shouldMove;
        if (shouldMove)
        {
            float resolvedDirection = direction != 0 ? Mathf.Sign(direction) : GetInitialDirection();
            currentDirection = resolvedDirection;
        }
        else
        {
            currentDirection = 0f;
        }
        hasDecided = true;
    }
}

