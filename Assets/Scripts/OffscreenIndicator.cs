using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class OffscreenIndicator : MonoBehaviour
{
    [Header("Indicator Settings")]
    public RectTransform indicatorPrefab;
    public string playerTag = "Player";
    public Camera mainCamera;
    public float edgePadding = 50f;

    private List<RectTransform> indicators = new List<RectTransform>();
    private Transform[] targets;

    private void Start()
    {
        UpdateTargets();
    }

    private void Update()
    {
        if (mainCamera == null || targets == null) return;

        for (int i = 0; i < targets.Length; i++)
        {
            Transform target = targets[i];
            RectTransform indicatorUI = indicators[i];

            if (target == null)
            {
                indicatorUI.gameObject.SetActive(false);
                continue;
            }

            Vector3 screenPosition = mainCamera.WorldToScreenPoint(target.position);
            bool isOffscreen = screenPosition.x < 0 || screenPosition.x > Screen.width || screenPosition.y < 0 || screenPosition.y > Screen.height;

            indicatorUI.gameObject.SetActive(isOffscreen);

            if (isOffscreen)
            {
                UpdateIndicator(indicatorUI, target, screenPosition);
            }
        }
    }

    public void UpdateTargets()
    {
        ClearIndicators();
        targets = FindTargets(playerTag).ToArray();
        foreach (Transform target in targets)
        {
            indicators.Add(Instantiate(indicatorPrefab, transform));
        }
        Debug.Log($"Offscreen indicators updated with {targets.Length} targets.");
    }

    private void ClearIndicators()
    {
        foreach (var indicator in indicators)
        {
            if (indicator != null)
                Destroy(indicator.gameObject);
        }
        indicators.Clear();
    }

    private IEnumerable<Transform> FindTargets(string tag)
    {
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag(tag))
        {
            yield return obj.transform;
        }
    }

    private void UpdateIndicator(RectTransform indicatorUI, Transform target, Vector3 screenPosition)
    {
        // Clamp position to screen edges
        screenPosition.x = Mathf.Clamp(screenPosition.x, edgePadding, Screen.width - edgePadding);
        screenPosition.y = Mathf.Clamp(screenPosition.y, edgePadding, Screen.height - edgePadding);
        indicatorUI.position = screenPosition;

        // Rotate indicator to point toward the target
        Vector3 direction = (target.position - mainCamera.transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        indicatorUI.rotation = Quaternion.Euler(0, 0, angle - 90);

        // Update the character sprite rotation to match the prefab
        Transform characterPreviewTransform = indicatorUI.Find("CharacterPreview");
        if (characterPreviewTransform != null)
        {
            Image characterPreviewImage = characterPreviewTransform.GetComponent<Image>();
            if (characterPreviewImage != null)
            {
                SpriteRenderer targetSpriteRenderer = target.GetComponentInChildren<SpriteRenderer>();
                if (targetSpriteRenderer != null && targetSpriteRenderer.sprite != null)
                {
                    characterPreviewImage.sprite = targetSpriteRenderer.sprite; // Assign the sprite
                    characterPreviewImage.enabled = true; // Ensure the image is enabled
                    bool isFacingRight = target.localScale.x > 0;
                    characterPreviewImage.rectTransform.localScale = new Vector3(isFacingRight ? 1 : -1, 1, 1); // Flip based on facing direction
                    characterPreviewImage.rectTransform.rotation = targetSpriteRenderer.transform.rotation; // Match rotation of prefab
                }
                else
                {
                    Debug.LogWarning($"SpriteRenderer or sprite not found on target {target.name}. Disabling character preview.");
                    characterPreviewImage.enabled = false; // Disable the image if no sprite is found
                }
            }
            else
            {
                Debug.LogWarning("CharacterPreview Image component not found in indicator.");
            }
        }
        else
        {
            Debug.LogWarning("CharacterPreview transform not found in indicator.");
        }
    }
}