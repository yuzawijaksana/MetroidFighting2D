using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SafeZone : MonoBehaviour
{
    [Tooltip("When true, calls SetControllable(false) on the player's PlayerController when inside the zone.")]
    public bool disableControl = true;

    [Tooltip("When true, freezes the player's Rigidbody2D while inside the zone (restores on exit).")]
    public bool freezeMovement = true;

    [Tooltip("Only react to objects with this tag when enabled.")]
    public bool useTagFilter = false;
    public string playerTag = "Player";

    // Store previous Rigidbody2D states so we can restore them on exit
    private readonly Dictionary<int, RigidbodyConstraints2D> savedConstraints = new Dictionary<int, RigidbodyConstraints2D>();
    private readonly Dictionary<int, bool> savedSimulated = new Dictionary<int, bool>();

    private void OnValidate()
    {
        var col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            // Encourage using trigger; do not force-edit in edit-time, but set to true for convenience
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (useTagFilter && other.tag != playerTag) return;

        var pc = other.GetComponent<PlayerController>()
                 ?? other.GetComponentInParent<PlayerController>()
                 ?? other.GetComponentInChildren<PlayerController>();

        if (pc != null)
        {
            if (disableControl) pc.SetControllable(false);

            if (freezeMovement)
            {
                var rb = other.GetComponent<Rigidbody2D>() ?? other.GetComponentInParent<Rigidbody2D>() ?? other.GetComponentInChildren<Rigidbody2D>();
                if (rb != null)
                {
                    int id = rb.GetInstanceID();
                    if (!savedConstraints.ContainsKey(id)) savedConstraints[id] = rb.constraints;
                    if (!savedSimulated.ContainsKey(id)) savedSimulated[id] = rb.simulated;

                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                    rb.constraints = RigidbodyConstraints2D.FreezeAll;
                    rb.simulated = false;
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (useTagFilter && other.tag != playerTag) return;

        var pc = other.GetComponent<PlayerController>()
                 ?? other.GetComponentInParent<PlayerController>()
                 ?? other.GetComponentInChildren<PlayerController>();

        if (pc != null)
        {
            if (disableControl) pc.SetControllable(true);

            if (freezeMovement)
            {
                var rb = other.GetComponent<Rigidbody2D>() ?? other.GetComponentInParent<Rigidbody2D>() ?? other.GetComponentInChildren<Rigidbody2D>();
                if (rb != null)
                {
                    int id = rb.GetInstanceID();
                    if (savedConstraints.ContainsKey(id)) rb.constraints = savedConstraints[id];
                    if (savedSimulated.ContainsKey(id)) rb.simulated = savedSimulated[id];

                    savedConstraints.Remove(id);
                    savedSimulated.Remove(id);
                }
            }
        }
    }
}
