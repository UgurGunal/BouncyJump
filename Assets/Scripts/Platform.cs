using System.Collections;
using UnityEngine;

public class Platform : MonoBehaviour
{
    public float jumpForce = 30f;
    public float preBoost = 100f;
    public float boostMultiplier = 1.2f;
    public float destroyTime = 5f; // if circle reaches higher than a platform that platform will destroy after 5 sec
    private Transform target; // Reference to the character or object to track
    private Renderer platformRenderer; // Reference to the platform's Renderer component

    private void Start()
    {
        // Automatically find the character by tag (make sure the character has the "Player" tag)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }

        platformRenderer = GetComponent<Renderer>(); // Get the Renderer to change the color
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.relativeVelocity.y <= 0)
        {
            Character character = collision.collider.GetComponent<Character>();
            Rigidbody2D rb = collision.collider.GetComponent<Rigidbody2D>();

            if (rb != null && character != null)
            {
                if(rb.angularVelocity <= 0)
                {
                    rb.angularVelocity -= preBoost;
                }
                else
                {
                    rb.angularVelocity += preBoost;
                }
                rb.angularVelocity *= boostMultiplier;
                float jumpBoost = character.CalculateJumpBoost(); // Call function instead of using a property
                rb.velocity = new Vector2(rb.velocity.x, jumpForce + jumpBoost);
                //Debug.Log(rb.velocity.y);
            }
        }
    }

    private void Update()
    {
        if (target == null) return;

        if (target.position.y > 20 && target.position.y > transform.position.y)
        {
            StartCoroutine(DestroyPlatformWithColorChange(destroyTime)); // Start the coroutine when target moves above
        }
    }

    private IEnumerator DestroyPlatformWithColorChange(float delay)
    {
        float elapsedTime = 0f;
        Color initialColor = platformRenderer.material.color; // Store the initial color
        Color targetColor = Color.red; // Set the target color (you can choose any color)

        // Gradually change the color over the specified delay
        while (elapsedTime < delay)
        {
            platformRenderer.material.color = Color.Lerp(initialColor, targetColor, elapsedTime / delay);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the final color is set
        platformRenderer.material.color = targetColor;

        // Destroy the platform after the delay
        Destroy(gameObject);
    }
}
