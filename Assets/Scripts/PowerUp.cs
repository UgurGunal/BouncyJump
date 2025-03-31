using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public float angularSpeedIncrease = 500f;
    public float newAngularLimit = 5000f;// Increase in angular speed
    public float newJumpBoostMultiplier = 0.01f;
    public float duration = 4.8f; // How long the effect lasts

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) // Ensure it only affects the player
        {
            Character player = other.GetComponent<Character>();
            if (player != null)
            {
                player.ActivateAngularMomentumPowerUp(angularSpeedIncrease, newAngularLimit, duration);
                Destroy(gameObject); // Remove the power-up after collection
            }
        }
    }
}
