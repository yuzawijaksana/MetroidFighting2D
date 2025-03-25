using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Use the new Input System

public class OneWayPlatform : MonoBehaviour
{
    [Tooltip("Link to tutorial: https://youtu.be/7rCUt6mqqE8?si=ynfPsqW85V98CRtG")]
    public float waiting;
    private GameObject currentOneWayPlatform;

    [SerializeField] private BoxCollider2D playerCollider;

    // Update is called once per frame
    void Update()
    {
        // Use the new Input System to check for the "S" key press
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            if (currentOneWayPlatform != null)
            {
                Debug.Log("Starting DisableCollision coroutine");
                StartCoroutine(DisableCollision());
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("OneWayPlatform"))
        {
            Debug.Log("Collided with OneWayPlatform");
            currentOneWayPlatform = collision.gameObject;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("OneWayPlatform"))
        {
            Debug.Log("Exited collision with OneWayPlatform");
            currentOneWayPlatform = null;
        }
    }

    private IEnumerator DisableCollision()
    {
        BoxCollider2D platformCollider = currentOneWayPlatform.GetComponent<BoxCollider2D>();

        Debug.Log("Ignoring collision with platform");
        Physics2D.IgnoreCollision(playerCollider, platformCollider);
        yield return new WaitForSeconds(waiting);
        Debug.Log("Re-enabling collision with platform");
        Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
    }
}
