using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    private Rigidbody2D rb;
    private float moveX;
    public float maxSpeedBase = 14f;
    public float maxFallSpeed = -40f;
    public float maxForce = 180f;
    public float minForce = 20f;
    public float maxAngularVelocity = 2800f;
    public bool isPowerUpped = false;

    public float jumpBoostMultiplier = 0.01f;  // Multiplier for jump boost effect
    public float speedBoostMultiplier = 0.002f;
    private float activeAngularLimit;
    private int powerUpCount = 0; //  Track active power-ups
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        activeAngularLimit = maxAngularVelocity;
    }

    void Update()
    {
        moveX = Input.GetAxis("Horizontal");
    }

    private void FixedUpdate()
    {
        if (moveX != 0)
        {
            ApplyMovementForce(moveX);
        }
        float maxSpeed = maxSpeedBase + Mathf.Abs(rb.angularVelocity) * speedBoostMultiplier;
        // Clamp velocity
        rb.velocity = new Vector2(
            Mathf.Clamp(rb.velocity.x, -maxSpeed, maxSpeed),
            Mathf.Max(rb.velocity.y, maxFallSpeed) // Ensures Y velocity doesn't go below maxFallSpeed
        );

        rb.angularVelocity = Mathf.Clamp(rb.angularVelocity, -activeAngularLimit, activeAngularLimit);
    }

    void ApplyMovementForce(float direction)
    {
        float maxSpeed = maxSpeedBase + Mathf.Abs(rb.angularVelocity) * speedBoostMultiplier;
        float currentSpeed = rb.velocity.x;
        float speedRatio = Mathf.Abs(currentSpeed) / maxSpeed;
        bool isReversing = (currentSpeed * direction) < 0;
        float appliedForce = isReversing ? maxForce * 2 : Mathf.Lerp(maxForce, minForce, speedRatio);

        rb.AddForce(new Vector2(direction * appliedForce, 0f), ForceMode2D.Force);
    }

    // Function to calculate jump boost
    public float CalculateJumpBoost()
    {
        return Mathf.Abs(rb.angularVelocity) * jumpBoostMultiplier;
    }

    public void increaseAngularVelocity(float amount)
    {
        if(rb.angularVelocity <= 0)
        {
            rb.angularVelocity -= amount;
        }
        else
        {
            rb.angularVelocity += amount;
        }
    }

    public void ActivateAngularMomentumPowerUp(float angularBoost, float newAngularLimit, float duration)
    {
        // Start the coroutine from the player object
        StartCoroutine(AngularMomentumPowerUp(angularBoost, newAngularLimit, duration));
    }


    public IEnumerator AngularMomentumPowerUp(float angularBoost, float newAngularLimit, float duration)
    {
        isPowerUpped = true;
        powerUpCount++;  //  Increase active power-up count
        activeAngularLimit = newAngularLimit; // Set new limit
        float elapsedTime = 0f;
        Debug.Log("START POWER-UP");

        increaseAngularVelocity(2000);

        while (elapsedTime < duration)
        {
            yield return null;
            elapsedTime += Time.deltaTime;
            increaseAngularVelocity(angularBoost * Time.deltaTime);
        }

        Debug.Log("POWER-UP ENDED");

        powerUpCount--;  //  Decrease active power-up count

        //  Only reset if no more active power-ups
        if (powerUpCount == 0)
        {
            activeAngularLimit = maxAngularVelocity;
            isPowerUpped = false;
        }
    }


    //OLD FUNCTION NOT USED
    public IEnumerator OldAngularMomentumPowerUp(float angularBoost, float newAngularLimit, float newJumpBoostMultiplier, float duration)
    {
        //Record old Values
        float oldAngularLimit = maxAngularVelocity;
        float oldJumpBoostMultiplier = jumpBoostMultiplier;

        maxAngularVelocity = newAngularLimit; // Increase limit
        jumpBoostMultiplier = newJumpBoostMultiplier;
        increaseAngularVelocity(angularBoost);

        yield return new WaitForSeconds(duration);

        // Reset after duration
        maxAngularVelocity = oldAngularLimit;
        jumpBoostMultiplier = oldJumpBoostMultiplier;

    }
}
