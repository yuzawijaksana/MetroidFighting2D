using UnityEngine;
using System.Collections;


public class DeadzoneHandler : MonoBehaviour
{
    [SerializeField] private Vector3 teleportPosition = Vector3.zero;

    // Handles collision with the deadzone and teleports the player
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"Collision detected with: {collision.gameObject.name}");

        Damageable damageable = collision.GetComponentInChildren<Damageable>();
        if (damageable != null)
        {
            damageable.ResetHealth();

            // If player is out of hearts, handle victory and set inactive
            if (damageable.maxHearts <= 0)
            {
                GameObject playerObj = collision.GetComponentInParent<PlayerController>()?.gameObject;
                if (playerObj != null)
                {
                    StartCoroutine(DeathDelay(playerObj, 0.75f));
                    return; // Don't teleport a dead player
                }
            }

            PlayerController playerController = collision.GetComponentInParent<PlayerController>();
            if (playerController != null && damageable.IsStunned()) playerController.isControllable = false;
            Debug.Log($"Player hit the Deadzone. Teleporting to {teleportPosition} and resetting health.");
        }

        StartCoroutine(SmoothTeleport(collision));
    }

    private IEnumerator DeathDelay(GameObject playerObj, float delay)
    {
        playerObj.SetActive(false);
        yield return new WaitForSeconds(delay);
        GameStarter.Instance?.OnPlayerDeath(playerObj);
    }

    // Smoothly teleports the player to the specified position
    private IEnumerator SmoothTeleport(Collider2D collision)
    {
        // Cache the transform and Rigidbody2D at the start
        var cachedTransform = collision != null ? collision.transform : null;
        var rb = collision != null ? collision.GetComponent<Rigidbody2D>() : null;
        var playerController = collision != null ? collision.GetComponentInParent<PlayerController>() : null;

        if (playerController != null) 
        {
            playerController.HandleRespawning(true); // Start respawning
            playerController.SetControllable(false); // Disable control during teleport
        }
        Vector3 startPosition = cachedTransform != null ? cachedTransform.position : Vector3.zero;
        float elapsedTime = 0f;
        float duration = 3f;

        while (elapsedTime < duration)
        {
            if (cachedTransform == null)
                yield break; // Exit if the object was destroyed

            cachedTransform.position = Vector3.Lerp(startPosition, teleportPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (cachedTransform != null)
            cachedTransform.position = teleportPosition;

        // Reset vertical velocity to simulate slow falling
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -2f); // Slow falling velocity
        }

        // Wait until the player touches the ground
        if (playerController != null)
        {
            while (!playerController.Grounded())
            {
                yield return null; // Wait for the next frame
            }

            playerController.HandleRespawning(false); // End respawning
            playerController.SetControllable(true); // Re-enable control
        }
    }
}