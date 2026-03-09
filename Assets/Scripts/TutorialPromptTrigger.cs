using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class TutorialPromptTrigger : MonoBehaviour
{
    [Header("Serialized Settings")]
    [SerializeField] private string actionName = ""; 
    [SerializeField] private int bindingIndex = 0; 
    [SerializeField] private string actionDescription = "";

    [Header("Trigger Logic")]
    [SerializeField] private bool triggerOnlyOnce = true;
    [SerializeField] private bool hasTriggered = false; // Check this in Inspector!

    [Header("UI References (RE-DRAG FOR EACH COPY)")]
    [SerializeField] private GameObject promptUIContainer;
    [SerializeField] private RectTransform backgroundRect; 
    [SerializeField] private TextMeshProUGUI buttonTextField;
    [SerializeField] private TextMeshProUGUI descriptionTextField;
    [SerializeField] private CanvasGroup canvasGroup;

    private bool playerInRange = false;
    private InputAction currentAction;

    private void Awake()
    {
        // Force the UI to be off at start
        if (promptUIContainer != null) 
        {
            promptUIContainer.SetActive(false);
            canvasGroup.alpha = 0;
        }

        if (InputRebindingManager.Instance != null && !string.IsNullOrEmpty(actionName))
            currentAction = InputRebindingManager.Instance.gameInputs.Player.Get().FindAction(actionName);
    }

    private void Update()
    {
        // If we already finished this tutorial, do nothing
        if (triggerOnlyOnce && hasTriggered) return;

        // If player performs the action, mark as finished and fade out
        if (playerInRange && currentAction != null && currentAction.WasPressedThisFrame())
        {
            Debug.Log($"[Tutorial] Action {actionName} performed! Dimissing {gameObject.name}.");
            hasTriggered = true; 
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(0));
        }
    }

    public void RefreshLayout()
    {
        if (currentAction == null) return;

        // 1. Set text from the specific Inspector values for THIS object
        buttonTextField.text = currentAction.GetBindingDisplayString(bindingIndex);
        descriptionTextField.text = actionDescription;

        // 2. Force the UI to resize
        Canvas.ForceUpdateCanvases();
        if (backgroundRect != null) 
            LayoutRebuilder.ForceRebuildLayoutImmediate(backgroundRect);
        
        // Use gameObject.name to see which specific trigger is firing
        Debug.Log($"[Tutorial] {gameObject.name} Refreshed. Action: {actionName}, Desc: {actionDescription}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // If it's already triggered, don't show it again
        if (triggerOnlyOnce && hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            RefreshLayout(); // Update text EVERY time we enter
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(1));
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(0));
        }
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (targetAlpha > 0) promptUIContainer.SetActive(true);
        
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0;
        float duration = 0.25f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        if (targetAlpha <= 0) promptUIContainer.SetActive(false);
    }
}