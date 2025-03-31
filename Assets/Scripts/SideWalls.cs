using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallBoostManager : MonoBehaviour
{
    public float boostMultiplier = 1.4f; // Boost for multiplier
    public float cooldownTime = 0.5f;    // Cooldown before applying boost again

    private float cooldownCounter = 0f; // Timer to track cooldown

    private void Update()
    {
        // Reduce cooldown over time
        if (cooldownCounter > 0)
        {
            cooldownCounter -= Time.deltaTime;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D rb = collision.collider.GetComponent<Rigidbody2D>();

        if (rb != null && cooldownCounter <= 0)
        {
            

            // Apply the boost
            rb.angularVelocity *= boostMultiplier;

            //// Determine boost based on wall's tag
            //if (CompareTag("RightWall"))
            //{
            //    if (rb.angularVelocity > 0)
            //    {
            //        rb.angularVelocity *= -1;
            //    }
            //}
            //else if (CompareTag("LeftWall"))
            //{
            //    if(rb.angularVelocity < 0)
            //    {
            //        rb.angularVelocity *= -1;
            //    }
            //}

            // Start cooldown
            cooldownCounter = cooldownTime;
        }
    }
}
