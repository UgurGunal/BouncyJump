using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainPlatform : MonoBehaviour
{
    public float jumpForce = 33f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.relativeVelocity.y <= 0)
        {
            Character character = collision.collider.GetComponent<Character>();
            Rigidbody2D rb = collision.collider.GetComponent<Rigidbody2D>();

            if (rb != null && character != null)
            {
                float jumpBoost = character.CalculateJumpBoost(); // Call function instead of using a property
                rb.velocity = new Vector2(rb.velocity.x, jumpForce + jumpBoost);
            }
        }
    }
}
