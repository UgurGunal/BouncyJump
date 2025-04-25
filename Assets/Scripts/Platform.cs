using System.Collections;
using UnityEngine;

public class Platform : MonoBehaviour
{
    private PointsManager pointsManager;

    [SerializeField] private float jumpForce = 30f;
    [SerializeField] private float preBoost = 100f;
    [SerializeField] private float boostMultiplier = 1.2f;
    [SerializeField] private float destroyTime = 0.5f; // Duration of the shake
    [SerializeField] private float shakeMagnitude = 0.7f; // Magnitude of the shake

    private Transform target;
    private bool isBeingDestroyed = false;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
    }


    private void SecondOnCollisionEnter2D(Collision2D collision)
    {
        // Get the player's rigidbody
        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        Character character = collision.collider.GetComponent<Character>();

        if (playerRb != null)
        {
   

            // Debug information

            if (collision.relativeVelocity.y < 0)
            {
                // Apply angular velocity modifications
                if (playerRb.angularVelocity <= 0)
                    playerRb.angularVelocity -= preBoost;
                else
                    playerRb.angularVelocity += preBoost;

                playerRb.angularVelocity *= boostMultiplier;

                // Calculate jump boost
                float jumpBoost = 0;
                if (character != null)
                {
                    jumpBoost = character.CalculateJumpBoost();
                }

                // Apply the velocity change to make the player jump
                playerRb.velocity = new Vector2(playerRb.velocity.x, jumpForce + jumpBoost);
            }
        }
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Get the player's rigidbody
        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        Character character = collision.collider.GetComponent<Character>();

        if (playerRb != null)
        {
            // Check collision contact points to determine direction
            bool collidingFromTop = false;

            foreach (ContactPoint2D contact in collision.contacts)
            {
                // If contact normal points downward, character is hitting from above
                if (contact.normal.y < -0.5f)  // Platform's normal pointing down means object is above
                {
                    collidingFromTop = true;
                    break;
                }
            }

            // Debug information
            Debug.Log($"Colliding from top: {collidingFromTop}, RelVelocity: {collision.relativeVelocity.y}");

            if (collidingFromTop)
            {
                
                    if (pointsManager == null)
                    {
                        pointsManager = FindObjectOfType<PointsManager>();
                    }

                    pointsManager?.UpdateUI();

                    // Apply angular velocity modifications
                    if (playerRb.angularVelocity <= 0)
                        playerRb.angularVelocity -= preBoost;
                    else
                        playerRb.angularVelocity += preBoost;

                    playerRb.angularVelocity *= boostMultiplier;

                    // Calculate jump boost
                    float jumpBoost = 0;
                    if (character != null)
                    {
                        jumpBoost = character.CalculateJumpBoost();
                    }

                    // Apply the velocity change to make the player jump
                    playerRb.velocity = new Vector2(playerRb.velocity.x, jumpForce + jumpBoost);
                
               
            }
        }
    }



    public IEnumerator DestroyPlatformWithShake()
    {
        if (!isBeingDestroyed)
        {
            isBeingDestroyed = true;
            Vector3 originalPosition = transform.position;
            float elapsedTime = 0f;

            // Calculate phase durations
            float phase1Duration = destroyTime * 0.3f;
            float phase2Duration = destroyTime * 0.35f;
            float phase3Duration = destroyTime * 0.35f;
            float phase2ShakeInterval = 0.05f; // Higher = slower shake
            float phase3ShakeInterval = 0.012f; // Lower = faster shake

            float shakeTimer = 0f;
            Vector3 currentShakeOffset = Vector3.zero;

            while (elapsedTime < destroyTime)
            {
                float normalizedTime = elapsedTime / (phase1Duration + phase2Duration + phase3Duration);

                if (elapsedTime < phase1Duration)
                {
                    // Phase 1: No shaking
                    currentShakeOffset = Vector3.zero;
                    transform.position = originalPosition;
                }
                else if (elapsedTime < phase1Duration + phase2Duration)
                {
                    // Phase 2: Small shake, low frequency
                    shakeTimer += Time.deltaTime;
                    if (shakeTimer >= phase2ShakeInterval)
                    {
                        shakeTimer = 0f;
                        currentShakeOffset = new Vector3(
                            Random.Range(-shakeMagnitude / 2, shakeMagnitude / 2f),
                            Random.Range(-shakeMagnitude / 2, shakeMagnitude / 2f),
                            0
                        );
                    }
                    transform.position = originalPosition + currentShakeOffset;
                }
                else
                {
                    // Phase 3: Big shake, high frequency
                    shakeTimer += Time.deltaTime;
                    if (shakeTimer >= phase3ShakeInterval)
                    {
                        shakeTimer = 0f;
                        currentShakeOffset = new Vector3(
                            Random.Range(-shakeMagnitude, shakeMagnitude),
                            Random.Range(-shakeMagnitude, shakeMagnitude),
                            0
                        );
                    }
                    transform.position = originalPosition + currentShakeOffset;
                }


                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Reset the platform position to its original position after shake
            Destroy(gameObject);
        }
    }


    // Reset platform for reuse
    public void ResetPlatform()
    {
        isBeingDestroyed = false;
    }

}


//if (!isBeingDestroyed)
//{
//    isBeingDestroyed = true;
//    Vector3 originalPosition = transform.position;
//    float elapsedTime = 0f;

//    // Calculate phase durations
//    float phase1Duration = destroyTime * 0.3f;
//    float phase2Duration = destroyTime * 0.3f;
//    float phase3Duration = destroyTime * 0.4f;
//    float phase2Frequency = 10f; // Medium frequency
//    float phase3Frequency = 25f; // High frequency

//    while (elapsedTime < destroyTime)
//    {
//        float normalizedTime = elapsedTime / (phase1Duration + phase2Duration + phase3Duration);

//        if (elapsedTime < phase1Duration)
//        {
//            // Phase 1: No shaking
//            transform.position = originalPosition;
//        }
//        else if (elapsedTime < phase1Duration + phase2Duration)
//        {
//            // Phase 2: Mild shake
//            float t = (elapsedTime - phase1Duration) * phase2Frequency;
//            float offsetX = Mathf.Sin(t) * (shakeMagnitude / 3f);
//            float offsetY = Mathf.Cos(t) * (shakeMagnitude / 3f);
//            transform.position = originalPosition + new Vector3(offsetX, offsetY, 0);
//        }
//        else
//        {
//            // Phase 3: Stronger, faster shake
//            float t = (elapsedTime - phase1Duration - phase2Duration) * phase3Frequency;
//            float offsetX = Mathf.Sin(t) * shakeMagnitude;
//            float offsetY = Mathf.Cos(t) * shakeMagnitude;
//            transform.position = originalPosition + new Vector3(offsetX, offsetY, 0);
//        }

//        elapsedTime += Time.deltaTime;
//        yield return null;
//    }

//    // Reset the platform position to its original position after shake
//    Destroy(gameObject);
//}
