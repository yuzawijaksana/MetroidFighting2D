using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GroundClipFixer : MonoBehaviour
{
    [SerializeField] private string characterTag = "Player";
    [SerializeField] private float pushDistance = 0.1f;
    [SerializeField] private int maxPushIterations = 20;

    private Collider2D groundCollider;

    private void Awake()
    {
        groundCollider = GetComponent<Collider2D>();
        groundCollider.isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(characterTag)) return;
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        float groundTop = groundCollider.bounds.max.y;
        float characterBottom = other.bounds.min.y;

        if (characterBottom < groundTop)
        {
            int iterations = 0;
            Bounds charBounds = other.bounds;
            Bounds groundBounds = groundCollider.bounds;

            while (charBounds.Intersects(groundBounds) && iterations < maxPushIterations)
            {
                rb.position += Vector2.up * pushDistance;
                rb.linearVelocity = Vector2.zero;
                charBounds.center += Vector3.up * pushDistance;
                iterations++;
            }
        }
    }
}
