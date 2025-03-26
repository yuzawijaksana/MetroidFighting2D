using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AttackHitbox : NetworkBehaviour
{
    public enum KnockbackDirection
    {
        Up,
        Down,
        Side,
        SideUp,
        SideDown
    }

    public PlayerAttack.AttackType attackType;
    public float damage;
    public KnockbackDirection knockbackDirection;
    public KnockbackDirection airKnockbackDirection; // Knockback direction for airborne targets
    public float groundedKnockbackForce = 5f; // Knockback force for grounded targets
    public float airKnockbackForce = 7.5f; // Knockback force for airborne targets
    public float airKnockbackDelay = 0.2f; // Delay before applying air knockback

    private HashSet<GameObject> hitObjects = new HashSet<GameObject>(); // Track objects already hit
    private PlayerController playerController; // Reference to PlayerController

    private void Start()
    {
        // Get the PlayerController from the parent or attached GameObject
        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerController not found. Ensure the AttackHitbox is a child of a GameObject with a PlayerController.");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer)
        {
            // Send the collision event to the server for processing
            NetworkObject targetNetworkObject = collision.GetComponentInParent<NetworkObject>();
            if (targetNetworkObject != null)
            {
                ProcessHitServerRpc(targetNetworkObject.NetworkObjectId);
            }
            return;
        }

        ProcessHit(collision);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ProcessHitServerRpc(ulong targetNetworkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out var targetObject))
        {
            Collider2D targetCollider = targetObject.GetComponentInChildren<Collider2D>();
            if (targetCollider != null)
            {
                ProcessHit(targetCollider);
            }
        }
    }

    private void ProcessHit(Collider2D collision)
    {
        if (collision == null)
        {
            Debug.LogWarning("Collision object is null. Skipping hit processing.");
            return;
        }

        // Ignore collisions with the player's own GameObject
        if (collision.gameObject == playerController?.gameObject)
        {
            Debug.LogWarning("Collision with own player detected. Skipping.");
            return;
        }

        if (hitObjects.Contains(collision.gameObject))
        {
            Debug.LogWarning($"Collision with {collision.gameObject.name} already processed. Skipping.");
            return; // Skip if the object has already been hit
        }

        // Search for the Damageable component in the collision object or its children
        Damageable target = collision.GetComponentInChildren<Damageable>();
        if (target == null)
        {
            Debug.LogWarning($"No Damageable component found on {collision.gameObject.name} or its hierarchy. Skipping.");
            return;
        }

        // Determine if the target is grounded or airborne
        bool isTargetGrounded = target.IsGrounded();
        Vector2 knockback = isTargetGrounded
            ? GetKnockbackDirection(target.transform.position, knockbackDirection) * groundedKnockbackForce
            : GetKnockbackDirection(target.transform.position, airKnockbackDirection) * airKnockbackForce;

        // Apply damage and knockback on the server
        target.TakeDamage(damage, knockback);

        // Notify all clients about the knockback
        NetworkObject targetNetworkObject = target.GetComponentInParent<NetworkObject>();
        if (targetNetworkObject != null)
        {
            NotifyKnockbackClientRpc(targetNetworkObject.NetworkObjectId, knockback);
        }
        else
        {
            Debug.LogWarning($"No NetworkObject found for {collision.gameObject.name}. Knockback notification skipped.");
        }

        // Log when a player hits another player
        ulong targetId = targetNetworkObject != null ? targetNetworkObject.NetworkObjectId : 0;
        bool hasValidTarget = targetNetworkObject != null;
        Debug.Log($"Player {playerController?.NetworkObjectId} hit Player {targetId} for {damage} damage.");

        // Notify all clients about the hit
        NotifyHitClientRpc(playerController?.NetworkObjectId ?? 0, targetId, hasValidTarget, damage);

        hitObjects.Add(collision.gameObject);
    }

    [ClientRpc]
    private void NotifyHitClientRpc(ulong attackerId, ulong targetId, bool hasValidTarget, float damage)
    {
        if (hasValidTarget)
        {
            Debug.Log($"Player {attackerId} hit Player {targetId} for {damage} damage.");
        }
        else
        {
            Debug.Log($"Player {attackerId} hit an unknown target for {damage} damage.");
        }
    }

    [ClientRpc]
    private void NotifyKnockbackClientRpc(ulong targetNetworkObjectId, Vector2 knockback)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out var targetObject))
        {
            // Search for the Damageable component on the target object or its children
            Damageable target = targetObject.GetComponentInChildren<Damageable>();
            if (target != null)
            {
                target.ApplyKnockback(knockback);
            }
        }
    }

    private void ApplyKnockback(Damageable target, KnockbackDirection directionType, float force)
    {
        if (target == null || target.gameObject == null)
        {
            return;
        }

        Vector2 direction = GetKnockbackDirection(target.transform.position, directionType) * force;

        // Retrieve Rigidbody2D from the parent of the Damageable component
        Rigidbody2D targetRigidbody = target.GetComponentInParent<Rigidbody2D>();
        if (targetRigidbody != null)
        {
            // Reset velocity before applying knockback to avoid conflicts
            targetRigidbody.linearVelocity = Vector2.zero;

            // Apply knockback as an impulse
            targetRigidbody.AddForce(direction, ForceMode2D.Impulse);
        }
    }

    private IEnumerator ApplyAirKnockbackAfterDelay(Damageable target)
    {
        yield return new WaitForSeconds(airKnockbackDelay);

        // Check if the target is null or destroyed before accessing it
        if (target == null || target.gameObject == null)
        {
            yield break;
        }

        // Check if the target is still airborne before applying air knockback
        if (!target.IsGrounded())
        {
            ApplyKnockback(target, airKnockbackDirection, airKnockbackForce);
        }
    }

    private void ResetHitbox()
    {
        // Reset the hitbox state to ensure it can detect collisions again
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false; // Temporarily disable the collider
            collider.enabled = true;  // Re-enable the collider to reset its state
        }

        // Clear the hitObjects set to allow new hits in the next attack
        hitObjects.Clear();
    }

    public void ResetHitObjects()
    {
        hitObjects.Clear();
        Debug.Log(IsServer ? "Hit objects have been reset on the server." : "Hit objects have been reset on the client.");

        if (IsServer)
        {
            NotifyResetHitObjectsClientRpc(); // Notify all clients to reset their hitObjects
        }
        else
        {
            ResetHitObjectsServerRpc(); // Notify the server to reset its hitObjects
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetHitObjectsServerRpc()
    {
        hitObjects.Clear();
        Debug.Log("Hit objects have been reset on the server by a client.");
        NotifyResetHitObjectsClientRpc(); // Notify all clients to reset their hitObjects
    }

    [ClientRpc]
    private void NotifyResetHitObjectsClientRpc()
    {
        hitObjects.Clear();
        Debug.Log("Hit objects have been reset on the client.");
    }

    private IEnumerator ResetHitObjectsAfterDuration(float duration)
    {
        if (IsServer)
        {
            yield return new WaitForSeconds(duration);
            ResetHitObjects(); // Ensure this is only called on the server
        }
    }

    private void OnEnable()
    {
        // Delay the ResetHitObjects call to ensure the object is fully initialized
        StartCoroutine(DelayedResetHitObjects());
    }

    private IEnumerator DelayedResetHitObjects()
    {
        // Wait until the object is fully initialized and spawned
        yield return new WaitUntil(() => IsSpawned);

        ResetHitObjects();
    }

    private void OnDisable()
    {
        // Reset hit objects when the hitbox is disabled
        ResetHitObjects();
    }

    private Vector2 GetKnockbackDirection(Vector3 targetPosition, KnockbackDirection directionType)
    {
        // Use PlayerController's facing direction
        float facingDirection = playerController != null && playerController.isFacingRight ? 1 : -1;

        switch (directionType)
        {
            case KnockbackDirection.Up:
                return Vector2.up;
            case KnockbackDirection.Down:
                return Vector2.down;
            case KnockbackDirection.Side:
                return new Vector2(facingDirection, 0).normalized;
            case KnockbackDirection.SideUp:
                return new Vector2(facingDirection, 0.5f).normalized;
            case KnockbackDirection.SideDown:
                return new Vector2(facingDirection, -0.5f).normalized;
            default:
                return Vector2.zero;
        }
    }

    private void FlipTargetToFaceAttack(Transform targetTransform)
    {
        // Flip the parent of the target to face the direction of the attack
        Transform parentTransform = targetTransform.parent;
        if (parentTransform != null)
        {
            float attackDirection = playerController != null && playerController.isFacingRight ? -1 : 1;
            parentTransform.localScale = new Vector3(Mathf.Abs(parentTransform.localScale.x) * attackDirection, parentTransform.localScale.y, parentTransform.localScale.z);
        }
    }

    public void StartAttack(float duration)
    {
        // Start the attack and reset hit objects after the duration
        StartCoroutine(ResetHitObjectsAfterDuration(duration));
    }
}
