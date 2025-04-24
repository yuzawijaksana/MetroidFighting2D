using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OffscreenIndicator : MonoBehaviour
{
    [Header("Indicator Settings")]
    public RectTransform indicatorPrefab;
    public Transform[] targets;
    public Camera mainCamera;

    [Header("Indicator Appearance")]
    public float edgePadding = 50f;

    private List<RectTransform> indicators = new List<RectTransform>();

    private void Start()
    {
        // Automatically find all GameObjects tagged as "Player"
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        targets = new Transform[playerObjects.Length];
        for (int i = 0; i < playerObjects.Length; i++)
        {
            targets[i] = playerObjects[i].transform;
        }

        foreach (Transform target in targets)
        {
            indicators.Add(Instantiate(indicatorPrefab, transform));
        }
    }

    private void Update()
    {
        if (mainCamera == null) return;

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

        // Ensure character preview remains upright and flip only the character preview
        Transform characterPreviewTransform = indicatorUI.Find("CharacterPreview");
        if (characterPreviewTransform != null)
        {
            Image characterPreviewImage = characterPreviewTransform.GetComponent<Image>();
            if (characterPreviewImage != null)
            {
                characterPreviewImage.rectTransform.rotation = Quaternion.identity;

                // Update sprite and flip based on target's facing direction
                SpriteRenderer targetSpriteRenderer = target.GetComponentInChildren<SpriteRenderer>();
                if (targetSpriteRenderer != null)
                {
                    characterPreviewImage.sprite = targetSpriteRenderer.sprite;
                    bool isFacingRight = target.localScale.x > 0;
                    characterPreviewImage.rectTransform.localScale = new Vector3(isFacingRight ? 1 : -1, 1, 1);
                }
            }
        }

        // Rotate the background to point toward the target without flipping
        Transform backgroundTransform = indicatorUI.Find("Background");
        if (backgroundTransform != null)
        {
            Vector3 backgroundDirection = (target.position - mainCamera.transform.position).normalized;
            float backgroundAngle = Mathf.Atan2(backgroundDirection.y, backgroundDirection.x) * Mathf.Rad2Deg;
            backgroundTransform.rotation = Quaternion.Euler(0, 0, backgroundAngle - 90);

            // Ensure the background's local scale remains unaffected by character flipping
            backgroundTransform.localScale = Vector3.one;
        }

        // Scale indicator based on distance and normalize for screen resolution
        float distance = Vector3.Distance(mainCamera.transform.position, target.position);
        float scale = Mathf.Lerp(1f, 0.5f, Mathf.InverseLerp(5f, 15f, distance));
        float resolutionFactor = Screen.height / 320f; // Assuming 1080p as the reference resolution
        indicatorUI.localScale = new Vector3(scale * resolutionFactor, scale * resolutionFactor, 1);
    }
}