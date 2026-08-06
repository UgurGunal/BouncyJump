using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles all player-related particle effects (wall dust at the wall and bounce particles on the player).
/// Keeps PlayerBallController focused on movement/logic.
/// </summary>
public class PlayerParticleController : MonoBehaviour
{
    const int DefaultPoolPrewarm = 4;

    [Header("Wall Bounce Particles")]
    [Tooltip("Primary wall bounce particle system prefab to instantiate near the wall when bouncing.")]
    public ParticleSystem wallDustParticleSystemPrefab;
    [Tooltip("Secondary wall bounce particle system prefab (behaves exactly like the first, optional).")]
    public ParticleSystem wallDustParticleSystemPrefab2;
    [Tooltip("How many instances to prewarm per wall-dust prefab.")]
    [Min(1)] public int wallDustPoolPrewarm = DefaultPoolPrewarm;

    [Header("Powerup Particles")]
    [Tooltip("Prefab with child ParticleSystem components. Spawned on the player while a combo powerup is active.")]
    public GameObject powerupEffectPrefab;

    private Vector3 originalWallDustShapePosition;
    private Vector3 originalWallDustShapePosition2;
    private Vector3 originalWallDustShapeRotation;
    private Vector3 originalWallDustShapeRotation2;
    private Transform _playerTransform;
    private Transform _poolRoot;
    private GameObject _powerupEffectInstance;
    private ParticleSystem[] _powerupParticleSystems;
    private readonly Dictionary<int, Queue<ParticleSystem>> _wallDustPools = new Dictionary<int, Queue<ParticleSystem>>();
    private readonly Dictionary<ParticleSystem, ParticleSystem> _instanceToPrefab = new Dictionary<ParticleSystem, ParticleSystem>();

    void Awake()
    {
        _playerTransform = transform;
        _poolRoot = new GameObject("PlayerParticlePool").transform;
        _poolRoot.SetParent(transform, false);
        _poolRoot.gameObject.SetActive(false);

        CachePrefabShape(wallDustParticleSystemPrefab, out originalWallDustShapePosition, out originalWallDustShapeRotation);
        CachePrefabShape(wallDustParticleSystemPrefab2, out originalWallDustShapePosition2, out originalWallDustShapeRotation2);

        PrewarmPool(wallDustParticleSystemPrefab, wallDustPoolPrewarm);
        PrewarmPool(wallDustParticleSystemPrefab2, wallDustPoolPrewarm);
    }

    static void CachePrefabShape(ParticleSystem prefab, out Vector3 shapePosition, out Vector3 shapeRotation)
    {
        shapePosition = Vector3.zero;
        shapeRotation = Vector3.zero;
        if (prefab == null)
            return;

        var shape = prefab.shape;
        shapePosition = shape.position;
        shapeRotation = shape.rotation;
    }

    void PrewarmPool(ParticleSystem prefab, int count)
    {
        if (prefab == null || count <= 0)
            return;

        Queue<ParticleSystem> queue = GetOrCreatePool(prefab);
        for (int i = 0; i < count; i++)
            queue.Enqueue(CreatePooledInstance(prefab));
    }

    Queue<ParticleSystem> GetOrCreatePool(ParticleSystem prefab)
    {
        int key = prefab.GetInstanceID();
        if (!_wallDustPools.TryGetValue(key, out Queue<ParticleSystem> queue))
        {
            queue = new Queue<ParticleSystem>();
            _wallDustPools[key] = queue;
        }

        return queue;
    }

    ParticleSystem CreatePooledInstance(ParticleSystem prefab)
    {
        ParticleSystem instance = Instantiate(prefab, _poolRoot);
        instance.gameObject.SetActive(false);
        _instanceToPrefab[instance] = prefab;
        return instance;
    }

    ParticleSystem Rent(ParticleSystem prefab)
    {
        Queue<ParticleSystem> queue = GetOrCreatePool(prefab);
        ParticleSystem instance = queue.Count > 0 ? queue.Dequeue() : CreatePooledInstance(prefab);
        instance.transform.SetParent(null, false);
        instance.gameObject.SetActive(true);
        return instance;
    }

    void Return(ParticleSystem instance)
    {
        if (instance == null)
            return;

        if (!_instanceToPrefab.TryGetValue(instance, out ParticleSystem prefab) || prefab == null)
        {
            Destroy(instance.gameObject);
            return;
        }

        instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        instance.gameObject.SetActive(false);
        instance.transform.SetParent(_poolRoot, false);
        GetOrCreatePool(prefab).Enqueue(instance);
    }

    /// <summary>
    /// Called by PlayerBallController (and ultimately by SideWall) to spawn wall and player particles.
    /// </summary>
    public void TriggerWallDustParticles(SideWall.WallSide wallSide, float collisionSpeed)
    {
        SpawnWallDust(wallSide, collisionSpeed, wallDustParticleSystemPrefab, originalWallDustShapePosition, originalWallDustShapeRotation);
        SpawnWallDust(wallSide, collisionSpeed, wallDustParticleSystemPrefab2, originalWallDustShapePosition2, originalWallDustShapeRotation2);
    }

    void SpawnWallDust(
        SideWall.WallSide wallSide,
        float collisionSpeed,
        ParticleSystem prefab,
        Vector3 originalShapePosition,
        Vector3 originalShapeRotation)
    {
        if (prefab == null)
            return;

        ParticleSystem particleInstance = Rent(prefab);
        particleInstance.transform.SetPositionAndRotation(_playerTransform.position, Quaternion.identity);

        var shape = particleInstance.shape;
        Vector3 position = originalShapePosition;
        Vector3 rotation = originalShapeRotation;

        if (wallSide == SideWall.WallSide.Right)
        {
            position.x = 0.28f;
            rotation.z = -rotation.z;
            rotation.x = (rotation.x + 180f) % 360f;
        }
        else
        {
            position.x = -0.38f;
            rotation.z = Mathf.Abs(rotation.z);
            rotation.x = Mathf.Abs(rotation.x % 360f);
        }

        shape.position = position;
        shape.rotation = rotation;

        var renderer = particleInstance.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            Vector2 flip = renderer.flip;
            flip.x = (wallSide == SideWall.WallSide.Right) ? 1f : 0f;
            renderer.flip = flip;
        }

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

        var main = particleInstance.main;
        float maxLifetime = main.startLifetime.constantMax;
        if (maxLifetime <= 0f)
            maxLifetime = main.startLifetime.constant;
        if (maxLifetime <= 0f)
            maxLifetime = 2f;

        StartCoroutine(ReturnAfterDelay(particleInstance, maxLifetime + 0.5f));
    }

    IEnumerator ReturnAfterDelay(ParticleSystem instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        Return(instance);
    }

    public void StartPowerupEffect()
    {
        if (powerupEffectPrefab == null)
            return;

        if (_powerupEffectInstance == null)
        {
            _powerupEffectInstance = Instantiate(powerupEffectPrefab, _playerTransform.position, Quaternion.identity);
            _powerupParticleSystems = _powerupEffectInstance.GetComponentsInChildren<ParticleSystem>(true);
        }
        else
        {
            _powerupEffectInstance.SetActive(true);
            _powerupEffectInstance.transform.SetPositionAndRotation(_playerTransform.position, Quaternion.identity);
        }

        if (_powerupParticleSystems == null)
            return;

        for (int i = 0; i < _powerupParticleSystems.Length; i++)
        {
            ParticleSystem ps = _powerupParticleSystems[i];
            if (ps != null)
                ps.Play(true);
        }
    }

    void LateUpdate()
    {
        if (_powerupEffectInstance == null || !_powerupEffectInstance.activeSelf)
            return;

        _powerupEffectInstance.transform.SetPositionAndRotation(_playerTransform.position, Quaternion.identity);
    }

    public void StopPowerupEffect()
    {
        if (_powerupEffectInstance == null)
            return;

        if (_powerupParticleSystems != null)
        {
            for (int i = 0; i < _powerupParticleSystems.Length; i++)
            {
                ParticleSystem ps = _powerupParticleSystems[i];
                if (ps != null)
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        _powerupEffectInstance.SetActive(false);
    }

    void OnDisable()
    {
        StopPowerupEffect();
        StopAllCoroutines();

        // Return any active rented instances still in the world.
        List<ParticleSystem> active = null;
        foreach (var pair in _instanceToPrefab)
        {
            ParticleSystem instance = pair.Key;
            if (instance == null || !instance.gameObject.activeSelf)
                continue;

            if (active == null)
                active = new List<ParticleSystem>();
            active.Add(instance);
        }

        if (active == null)
            return;

        for (int i = 0; i < active.Count; i++)
            Return(active[i]);
    }

    void OnDestroy()
    {
        if (_powerupEffectInstance != null)
        {
            Destroy(_powerupEffectInstance);
            _powerupEffectInstance = null;
        }
    }
}
