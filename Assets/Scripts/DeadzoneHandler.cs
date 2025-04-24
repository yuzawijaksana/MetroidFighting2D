using UnityEngine;

public class DeadzoneHandler : MonoBehaviour
{
    [SerializeField] private Vector3 teleportPosition = Vector3.zero; // Position to teleport the player to

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"Collision detected with: {collision.name}"); // Log the name of the object

        if (collision.CompareTag("Player")) // Ensure the object is the player
        {
            // Teleport the player and reset health
            Debug.Log($"Player hit the Deadzone. Teleporting to {teleportPosition} and resetting health.");
            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero; // Reset velocity
            }
            collision.transform.position = teleportPosition; // Teleport player

            Damageable damageable = collision.GetComponent<Damageable>();
            if (damageable != null)
            {
                damageable.ResetHealth(); // Reset health to 0
            }
        }
    }
}
