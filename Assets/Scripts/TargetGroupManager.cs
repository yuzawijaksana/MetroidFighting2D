using UnityEngine;
using Unity.Cinemachine;

public class TargetGroupManager : MonoBehaviour
{
    [Header("Target Group Settings")]
    [SerializeField] private CinemachineTargetGroup targetGroup;

    [Header("Player Tag")]
    [SerializeField] private string playerTag = "Player";

    private void Start()
    {
        if (targetGroup == null)
        {
            targetGroup = GetComponent<CinemachineTargetGroup>();
            if (targetGroup == null)
            {
                Debug.LogError("CinemachineTargetGroup component is not assigned or missing.");
                return;
            }
        }

        // Set CinemachineBrain to use unscaled time
        var brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
        if (brain != null)
        {
            // Use the public API properties, not internal fields
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.IgnoreTimeScale = true;
        }

        UpdateTargetGroup();
    }

    public void UpdateTargetGroup()
    {
        targetGroup.Targets.Clear();

        // Find all GameObjects tagged as "Players"
        GameObject[] targets = GameObject.FindGameObjectsWithTag(playerTag);

        // Add each target to the CinemachineTargetGroup
        foreach (GameObject target in targets)
        {
            Transform targetTransform = target.transform;
            targetGroup.AddMember(targetTransform, 1f, 2f); // Add with default weight and radius
        }

        Debug.Log($"Target group updated with {targets.Length} targets.");
    }
}
