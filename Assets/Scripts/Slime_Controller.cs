using UnityEngine;
using Pathfinding;
using System.Collections;

public class Slime_Controller : MonoBehaviour {
    public float upwardForce = 8f;
    public float forwardForce = 5f;
    public float jumpInterval = 2f;

    private IAstarAI ai;
    private Rigidbody2D rb;
    private bool isGrounded;

    void Start() {
        ai = GetComponent<IAstarAI>();
        rb = GetComponent<Rigidbody2D>();

        // Disable A* automatic movement so we can jump
        if (ai is AIPath aiPath) {
            aiPath.updatePosition = false;
            aiPath.updateRotation = false;
        }
        StartCoroutine(JumpCycle());
    }

    IEnumerator JumpCycle() {
        while (true) {
            yield return new WaitForSeconds(jumpInterval);
            if (ai.hasPath && isGrounded) {
                Vector2 direction = ((Vector2)ai.steeringTarget - (Vector2)transform.position).normalized;
                rb.AddForce(new Vector2(direction.x * forwardForce, upwardForce), ForceMode2D.Impulse);
                isGrounded = false;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        // Simple check: if we hit something, we are grounded
        isGrounded = true; 
    }
}