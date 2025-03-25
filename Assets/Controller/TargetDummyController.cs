using UnityEngine;
using System.Collections;

public class DummyController : MonoBehaviour
{
    private Animator anim;

    [Header("Dummy Animation Settings")]
    [SerializeField] private string spinAnimationTrigger = "Dummy_Spin";
    [SerializeField] private string idleAnimationTrigger = "Dummy_Idle";

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackResistance = 1f; // Reduce knockback effect
    [SerializeField] private Rigidbody2D parentRb; // Serialize the parent Rigidbody2D

    private void Start()
    {
        anim = GetComponent<Animator>();

        if (parentRb == null)
        {
            parentRb = GetComponent<Rigidbody2D>(); // Ensure Rigidbody2D is assigned
            if (parentRb == null)
            {
                Debug.LogError("Parent Rigidbody2D is not assigned or missing. Knockback will not work.");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the dummy is hit by an attack hitbox
        AttackHitbox hitbox = collision.GetComponent<AttackHitbox>();
        if (hitbox != null)
        {
            HandleHit(hitbox.attackType, hitbox.damage, collision.transform.position);
        }
    }

    private void HandleHit(PlayerAttack.AttackType attackType, float damage, Vector3 attackerPosition)
    {
        anim.SetTrigger(spinAnimationTrigger);
        StartCoroutine(ResetToIdleAfterDelay(1f)); // Adjust delay as needed
        Debug.Log($"Hit by {attackType} with {damage} damage.");

        // Apply knockback to the parent object
        if (parentRb != null)
        {
            Vector2 knockbackDirection = transform.position.x > attackerPosition.x ? Vector2.right : Vector2.left;
            parentRb.AddForce(knockbackDirection / knockbackResistance, ForceMode2D.Impulse);

            // Flip the parent to face the attacker
            float attackDirection = attackerPosition.x < transform.position.x ? -1 : 1;
            transform.parent.localScale = new Vector3(Mathf.Abs(transform.parent.localScale.x) * attackDirection, transform.parent.localScale.y, transform.parent.localScale.z);
            Debug.Log($"Flipped {transform.parent.name} to face attacker.");
        }
    }

    private IEnumerator ResetToIdleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        anim.SetTrigger(idleAnimationTrigger);
    }
}
