using System.Collections.Generic;
using UnityEngine;

public class Platform : MonoBehaviour
{
    static readonly List<Platform> ActivePlatforms = new List<Platform>();
    [Header("Jump Settings")]
    public float jumpForce = 14f;
    public float comboBonus = 0.005f; // Multiplier for current combo to jump bonus

    [Header("Collision Detection")]
    public float velocityThreshold = 5f; // Very lenient velocity check
    public float contactNormalThreshold = 0f; // Very lenient normal check

    [Header("Combo System")]
    public bool enableComboSystem = false; // Set to true if you want combo functionality

    [Header("Destruction Settings")]
    [Tooltip("Only destruction mode: removes platform when the player is this far above the platform's highest Y (not a timer).")]
    public bool enableDistanceDestroy = true;
    [Tooltip("Vertical gap (world units) between player and the platform's peak Y before Destroy is called.")]
    public float destroyDistance = 8f;

    [Header("Falling Settings")]
    [Tooltip("When enabled, platform falls downward after the player lands on it.")]
    public bool enableFalling = false;
    [Tooltip("Downward speed at the moment falling starts.")]
    public float fallMinSpeed = 1f;
    [Tooltip("Downward speed reached after acceleration time.")]
    public float fallMaxSpeed = 6f;
    [Tooltip("Seconds to accelerate from min to max fall speed.")]
    public float fallAccelerationTime = 1.5f;
    [Tooltip("Darken sprites toward black when falling (0 = none, 1 = fully black).")]
    [Range(0f, 1f)]
    public float fallTintStrength = 0.15f;

    [Header("Audio")]
    [Tooltip("If true, play the bouncy platform sound instead of the normal platform sound when the player jumps on this platform.")]
    public bool isBouncyPlatform = false;
    [Tooltip("If true, play anvil sound when the player collides with this platform from below.")]
    public bool isAnvil = false;
    [Tooltip("Optional: only this collider can trigger anvil audio. Leave empty to allow any collider on this platform.")]
    public Collider2D anvilAudioCollider;
    [Tooltip("Minimum time between anvil sound plays to avoid duplicate triggers from multi-collider contact.")]
    public float anvilSoundCooldown = 0.08f;

    private float lastAnvilSoundTime = -999f;
    private bool isFalling;
    private float fallElapsedTime;
    private float destroyReferenceY;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalSpriteColors;

    void RegisterInActiveList()
    {
        ActivePlatforms.Remove(this);
        float y = transform.position.y;
        int insertIndex = ActivePlatforms.Count;
        for (int i = 0; i < ActivePlatforms.Count; i++)
        {
            if (ActivePlatforms[i] == null)
                continue;

            if (ActivePlatforms[i].transform.position.y > y)
            {
                insertIndex = i;
                break;
            }
        }

        ActivePlatforms.Insert(insertIndex, this);
    }

    private void OnDisable()
    {
        ActivePlatforms.Remove(this);
    }

    private void Start()
    {
        destroyReferenceY = transform.position.y;
        CacheSpriteRenderers();
        RegisterInActiveList();
    }

    public static void ProcessAllLifecycles(Transform player)
    {
        if (player == null)
            return;

        float playerY = player.position.y;
        for (int i = ActivePlatforms.Count - 1; i >= 0; i--)
        {
            Platform platform = ActivePlatforms[i];
            if (platform == null)
            {
                ActivePlatforms.RemoveAt(i);
                continue;
            }

            platform.TickLifecycle(playerY);
        }
    }

    void TickLifecycle(float playerY)
    {
        destroyReferenceY = Mathf.Max(destroyReferenceY, transform.position.y);

        if (enableDistanceDestroy && playerY > destroyReferenceY + destroyDistance)
        {
            Despawn();
            return;
        }

        if (!isFalling)
            return;

        fallElapsedTime += Time.fixedDeltaTime;
        float t = fallAccelerationTime > 0f
            ? Mathf.Clamp01(fallElapsedTime / fallAccelerationTime)
            : 1f;
        float fallSpeed = Mathf.Lerp(fallMinSpeed, fallMaxSpeed, t);
        transform.position += Vector3.down * fallSpeed * Time.fixedDeltaTime;
    }

    void CacheSpriteRenderers()
    {
        spriteRenderers = GetComponents<SpriteRenderer>();
        originalSpriteColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            originalSpriteColors[i] = spriteRenderers[i].color;
    }

    public void ResetForSpawn(Vector3 worldPosition, Vector3 localScale)
    {
        isFalling = false;
        fallElapsedTime = 0f;
        lastAnvilSoundTime = -999f;
        transform.position = worldPosition;
        transform.localScale = localScale;
        transform.rotation = Quaternion.identity;
        destroyReferenceY = worldPosition.y;
        RestoreSpriteColors();
        RegisterInActiveList();
    }

    public void PrepareForPool()
    {
        isFalling = false;
        fallElapsedTime = 0f;
        RestoreSpriteColors();
    }

    void RestoreSpriteColors()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            CacheSpriteRenderers();

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
                continue;

            spriteRenderers[i].color = originalSpriteColors[i];
        }
    }

    void Despawn()
    {
        PooledInstance pooled = GetComponent<PooledInstance>();
        if (pooled != null)
            pooled.Release();
        else
            Destroy(gameObject);
    }

    public void StartFalling()
    {
        if (!enableFalling || isFalling)
            return;

        isFalling = true;
        fallElapsedTime = 0f;
        ApplyFallTint();
    }

    void ApplyFallTint()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            CacheSpriteRenderers();

        if (fallTintStrength <= 0f)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
                continue;

            Color baseColor = originalSpriteColors[i];
            spriteRenderers[i].color = Color.Lerp(baseColor, Color.black, fallTintStrength);
        }
    }

    public static void TriggerFallForPlatformsBelow(float collidedPlatformY)
    {
        for (int i = 0; i < ActivePlatforms.Count; i++)
        {
            Platform platform = ActivePlatforms[i];
            if (platform == null)
                continue;

            float platformY = platform.transform.position.y;
            if (platformY >= collidedPlatformY)
                break;

            platform.StartFalling();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        PlayerBallController player = PlayerBallController.Instance;
        if (player == null)
            player = collision.gameObject.GetComponent<PlayerBallController>();

        if (player == null)
            return;

        Rigidbody2D rb = player.Rigidbody;
        if (rb == null)
            rb = collision.rigidbody;

        HandleJump(player, rb, collision);
    }

    private void HandleJump(PlayerBallController player, Rigidbody2D rb, Collision2D collision)
    {
        TryPlayAnvilCollision(rb, collision);

        bool isOnTop = false;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < contactNormalThreshold)
            {
                isOnTop = true;
                break;
            }
        }

        if (!isOnTop)
            isOnTop = player.transform.position.y > transform.position.y;

        bool isNotMovingUp = rb.velocity.y <= velocityThreshold;

        if (isOnTop && isNotMovingUp)
        {
            float relativeVelocity = Mathf.Abs(collision.relativeVelocity.y);

            if (enableComboSystem)
                IncrementPlatformCombo(relativeVelocity);

            float jumpBonus = 0f;
            if (enableComboSystem)
                jumpBonus = GetComboBonus();

            float totalJumpForce = jumpForce + jumpBonus;
            player.Jump(totalJumpForce);

            if (SoundEffectsManager.Instance != null)
            {
                float pitchVariance = Random.Range(-0.1f, 0.1f);
                float pitch = 1f + pitchVariance;
                string soundName = isBouncyPlatform ? "bouncyPlatform" : "platform";
                SoundEffectsManager.Instance.PlaySound(soundName, -1f, pitch);
            }

            float collidedPlatformY = transform.position.y;
            StartFalling();
            TriggerFallForPlatformsBelow(collidedPlatformY);
            ChestPlatform.TriggerFallForChestsBelow(collidedPlatformY);
        }
    }

    private void TryPlayAnvilCollision(Rigidbody2D rb, Collision2D collision)
    {
        if (!isAnvil || SoundEffectsManager.Instance == null)
            return;

        if (Time.time - lastAnvilSoundTime < anvilSoundCooldown)
            return;

        if (!IsAllowedAnvilAudioCollider(collision))
            return;

        SoundEffectsManager.Instance.PlaySound("anvil");
        lastAnvilSoundTime = Time.time;
    }

    private bool IsAllowedAnvilAudioCollider(Collision2D collision)
    {
        if (anvilAudioCollider == null)
            return true;

        if (collision.otherCollider == anvilAudioCollider)
            return true;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.otherCollider == anvilAudioCollider)
                return true;
        }

        return false;
    }

    private void IncrementPlatformCombo(float relativeVelocity)
    {
        ComboManager combo = ComboManager.Instance;
        if (combo != null)
            combo.PlatformComboIncrement(relativeVelocity);
    }

    private float GetComboBonus()
    {
        ComboManager combo = ComboManager.Instance;
        if (combo == null)
            return 0f;

        return combo.getCombo() * comboBonus;
    }

    public void SetComboBonus(float bonus)
    {
        comboBonus = bonus;
    }

    public void EnableComboSystem(bool enable)
    {
        enableComboSystem = enable;
    }

    public void SetVelocityThreshold(float threshold)
    {
        velocityThreshold = threshold;
    }

    public void SetContactNormalThreshold(float threshold)
    {
        contactNormalThreshold = threshold;
    }
}
