using UnityEngine;

/// <summary>
/// Handles all player-related particle effects (wall dust at the wall and bounce particles on the player).
/// Keeps PlayerBallController focused on movement/logic.
/// </summary>
public class PlayerParticleController : MonoBehaviour
{
    [Header("Wall Bounce Particles")]
    [Tooltip("Primary wall bounce particle system prefab to instantiate near the wall when bouncing.")]
    public ParticleSystem wallDustParticleSystemPrefab;
    [Tooltip("Secondary wall bounce particle system prefab (behaves exactly like the first, optional).")]
    public ParticleSystem wallDustParticleSystemPrefab2;

    private Vector3 originalWallDustShapePosition;
    private Vector3 originalWallDustShapePosition2;
    private Transform _playerTransform;

    void Awake()
    {
        _playerTransform = transform;

        if (wallDustParticleSystemPrefab != null)
        {
            var shape = wallDustParticleSystemPrefab.shape;
            originalWallDustShapePosition = shape.position;
        }

        if (wallDustParticleSystemPrefab2 != null)
        {
            var shape2 = wallDustParticleSystemPrefab2.shape;
            originalWallDustShapePosition2 = shape2.position;
        }
    }

    /// <summary>
    /// Called by PlayerBallController (and ultimately by SideWall) to spawn wall and player particles.
    /// </summary>
    public void TriggerWallDustParticles(SideWall.WallSide wallSide, float collisionSpeed)
    {
        SpawnWallDust(wallSide, collisionSpeed, wallDustParticleSystemPrefab);
        SpawnWallDust(wallSide, collisionSpeed, wallDustParticleSystemPrefab2);
    }

    void SpawnWallDust(SideWall.WallSide wallSide, float collisionSpeed, ParticleSystem prefab)
    {
        if (prefab == null) return;

        // Instantiate a new particle system instance at the player's position
        var particleInstance = Instantiate(prefab, _playerTransform.position, Quaternion.identity);

        var shape = particleInstance.shape;

        // Set specific X positions based on wall side, starting from the prefab's own original shape position
        Vector3 position = (prefab == wallDustParticleSystemPrefab2) ? originalWallDustShapePosition2 : originalWallDustShapePosition;

        if (wallSide == SideWall.WallSide.Right)
        {
            position.x = 0.25f;

            // Flip rotation for right wall: mirror in Z and rotate 180° around X to invert emission direction
            Vector3 rotation = shape.rotation;
            rotation.z = -rotation.z;
            rotation.x = (rotation.x + 180f) % 360f;
            shape.rotation = rotation;
        }
        else
        {
            position.x = -0.3f;

            // Reset rotation for left wall (no X flip)
            Vector3 rotation = shape.rotation;
            rotation.z = Mathf.Abs(rotation.z);
            rotation.x = Mathf.Abs(rotation.x % 360f);
            shape.rotation = rotation;
        }

        shape.position = position;

        // Flip sprites based on wall side.
        // Note: TextureSheetAnimationModule.flipU is deprecated; use ParticleSystemRenderer.flip.x instead.
        // Kept here intentionally for the particle instance flip logic.
        // (We flip the renderer instead of using TextureSheetAnimationModule.flipU.)

        var renderer = particleInstance.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            Vector2 flip = renderer.flip;
            flip.x = (wallSide == SideWall.WallSide.Right) ? 1f : 0f;
            renderer.flip = flip;
        }

        // Calculate particle count based on collision speed
        float minSpeed = 8f;
        float maxSpeed = 12f;
        float minParticles = 1f;
        float maxParticles = 5f;

        float clampedSpeed = Mathf.Clamp(collisionSpeed, minSpeed, maxSpeed);
        float speedRatio = (clampedSpeed - minSpeed) / (maxSpeed - minSpeed);
        int particleCount = Mathf.RoundToInt(Mathf.Lerp(minParticles, maxParticles, speedRatio));
        particleCount = Mathf.Max(1, particleCount);

        particleInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particleInstance.Emit(particleCount);
        particleInstance.Play();

        // Destroy after lifetime + small buffer
        var main = particleInstance.main;
        float maxLifetime = main.startLifetime.constantMax;
        if (maxLifetime <= 0)
            maxLifetime = main.startLifetime.constant;
        if (maxLifetime <= 0)
            maxLifetime = 2f;

        Destroy(particleInstance.gameObject, maxLifetime + 0.5f);
    }

}

