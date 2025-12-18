using UnityEngine;

public class EnemyHurtbox : MonoBehaviour
{
    private Slime_Controller slimeController;
    
    void Start()
    {
        // Get the Slime_Controller from the parent
        slimeController = GetComponentInParent<Slime_Controller>();
        
        if (slimeController == null)
        {
            Debug.LogError("EnemyHurtbox could not find Slime_Controller in parent!");
        }
    }
    
    private void OnTriggerStay2D(Collider2D collision)
    {
        // Forward the trigger event to the parent controller
        if (slimeController != null)
        {
            slimeController.OnHurtboxTrigger(collision);
        }
    }
}
