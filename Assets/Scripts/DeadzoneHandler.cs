using UnityEngine;

public class DeadzoneHandler : MonoBehaviour
{
    [SerializeField] private Vector3 teleportPosition = Vector3.zero; // Position to teleport the player to

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"Collision detected with: {collision.gameObject.name}");

        Damageable damageable = collision.GetComponentInChildren<Damageable>();
        if (damageable != null)
        {
            // Reset health to 0
            damageable.ResetHealthTo(0);
            Debug.Log($"Player hit the Deadzone. Teleporting to {teleportPosition} and resetting health.");
        }

        // Teleport the player to the specified position
        collision.transform.position = teleportPosition;
    }
}
