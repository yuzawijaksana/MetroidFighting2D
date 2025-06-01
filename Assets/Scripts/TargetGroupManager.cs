using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

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

        UpdateTargets();
    }

    public void UpdateTargets()
    {
        targetGroup.Targets.Clear();
        foreach (Transform target in FindTargets(playerTag))
        {
            targetGroup.AddMember(target, 1f, 2f); // Add with default weight and radius
        }
        Debug.Log($"Target group updated with {targetGroup.Targets.Count} targets.");
    }

    public void ResetTargets()
    {
        targetGroup.Targets.Clear();
        Debug.Log("Target group has been reset.");
    }

    private IEnumerable<Transform> FindTargets(string tag)
    {
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag(tag))
        {
            yield return obj.transform;
        }
    }
}
