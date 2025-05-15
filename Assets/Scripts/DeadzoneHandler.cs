using UnityEngine;

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
            damageable.ResetHealthTo(0);

            PlayerController playerController = collision.GetComponentInParent<PlayerController>();
            if (playerController != null && damageable.IsStunned()) playerController.isControllable = false;
            Debug.Log($"Player hit the Deadzone. Teleporting to {teleportPosition} and resetting health.");
        }

        StartCoroutine(SmoothTeleport(collision));
    }

    // Smoothly teleports the player to the specified position
    private System.Collections.IEnumerator SmoothTeleport(Collider2D collision)
    {
        PlayerController playerController = collision.GetComponentInParent<PlayerController>();

        if (playerController != null) 
        {
            playerController.HandleRespawning(true); // Start respawning
            playerController.SetControllable(false); // Disable control during teleport
        }
        Vector3 startPosition = collision.transform.position;
        float elapsedTime = 0f;
        float duration = 3f;

        Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();

        while (elapsedTime < duration)
        {
            collision.transform.position = Vector3.Lerp(startPosition, teleportPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        collision.transform.position = teleportPosition;

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
