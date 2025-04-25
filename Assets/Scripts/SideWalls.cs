using System.Collections;
using UnityEngine;


[RequireComponent(typeof(Collider2D))]
public class WallBoostManager : MonoBehaviour
{
    public float boostMultiplier = 1.4f;
    public float cooldownTime = 0.5f;

    private bool isOnCooldown = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isOnCooldown || collision.rigidbody == null)
            return;

        collision.rigidbody.angularVelocity *= boostMultiplier;
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(cooldownTime);
        isOnCooldown = false;
    }
}
