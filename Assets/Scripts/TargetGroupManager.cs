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

        UpdateTargetGroup();
    }

    public void UpdateTargetGroup()
    {
        // Clear existing members
        targetGroup.m_Targets = new CinemachineTargetGroup.Target[0];

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
