using UnityEngine;
using System.Collections;

public class DummyController : MonoBehaviour
{
    private Animator anim;

    [Header("Dummy Animation Settings")]
    [SerializeField] private string spinAnimationTrigger = "Dummy_Spin";
    [SerializeField] private string idleAnimationTrigger = "Dummy_Idle";

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackResistance = 1f;
    [SerializeField] private Rigidbody2D parentRb;

    private void Start()
    {
        anim = GetComponent<Animator>();

        if (parentRb == null)
        {
            parentRb = GetComponent<Rigidbody2D>();
            if (parentRb == null)
            {
                Debug.LogError("Parent Rigidbody2D is not assigned or missing.");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        AttackHitbox hitbox = collision.GetComponent<AttackHitbox>();
        if (hitbox != null)
        {
            HandleHit(hitbox.damage, collision.transform.position); // Removed attackType reference
        }
    }

    private void HandleHit(float damage, Vector3 attackerPosition)
    {
        anim.SetTrigger(spinAnimationTrigger);
        StartCoroutine(ResetToIdleAfterDelay(1f));
        Debug.Log($"Hit with {damage} damage.");

        if (parentRb != null)
        {
            Vector2 knockbackDirection = transform.position.x > attackerPosition.x ? Vector2.right : Vector2.left;
            parentRb.AddForce(knockbackDirection / knockbackResistance, ForceMode2D.Impulse);

            float attackDirection = attackerPosition.x < transform.position.x ? -1 : 1;
            transform.parent.localScale = new Vector3(Mathf.Abs(transform.parent.localScale.x) * attackDirection, transform.parent.localScale.y, transform.parent.localScale.z);
        }
    }

    private IEnumerator ResetToIdleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        anim.SetTrigger(idleAnimationTrigger);
    }
}
