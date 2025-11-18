using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeTransition : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private Color fadeColor = Color.black;
    
    [Header("UI References")]
    [SerializeField] private Canvas fadeCanvas;
    [SerializeField] private Image fadeImage;
    
    private static FadeTransition instance;
    public static FadeTransition Instance 
    { 
        get 
        { 
            if (instance == null)
            {
                instance = FindFirstObjectByType<FadeTransition>();
                if (instance == null)
                {
                    instance = CreateFadeTransition();
                }
            }
            return instance; 
        } 
    }
    
    private bool isFading = false;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SetupFadeUI();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void SetupFadeUI()
    {
        // Create fade canvas if not assigned
        if (fadeCanvas == null)
        {
            GameObject canvasGO = new GameObject("FadeCanvas");
            canvasGO.transform.SetParent(transform);
            
            fadeCanvas = canvasGO.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 1000; // Very high to appear over everything
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Create fade image if not assigned
        if (fadeImage == null)
        {
            GameObject imageGO = new GameObject("FadeImage");
            imageGO.transform.SetParent(fadeCanvas.transform, false);
            
            fadeImage = imageGO.AddComponent<Image>();
            fadeImage.color = fadeColor;
            
            // Make it fullscreen
            RectTransform rect = fadeImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        
        // Start with transparent
        SetFadeAlpha(0f);
        fadeCanvas.gameObject.SetActive(false);
    }
    
    private static FadeTransition CreateFadeTransition()
    {
        GameObject fadeGO = new GameObject("FadeTransition");
        return fadeGO.AddComponent<FadeTransition>();
    }
    
    public void FadeOut(System.Action onComplete = null)
    {
        if (isFading) return;
        StartCoroutine(FadeCoroutine(0f, 1f, onComplete));
    }
    
    public void FadeIn(System.Action onComplete = null)
    {
        if (isFading) return;
        StartCoroutine(FadeCoroutine(1f, 0f, onComplete));
    }
    
    public void FadeOutAndIn(System.Action onMidFade = null, System.Action onComplete = null)
    {
        if (isFading) return;
        StartCoroutine(FadeOutAndInCoroutine(onMidFade, onComplete));
    }
    
    private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, System.Action onComplete)
    {
        isFading = true;
        fadeCanvas.gameObject.SetActive(true);
        
        float elapsedTime = 0f;
        float duration = 1f / fadeSpeed;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            SetFadeAlpha(alpha);
            yield return null;
        }
        
        SetFadeAlpha(endAlpha);
        
        if (endAlpha <= 0f)
        {
            fadeCanvas.gameObject.SetActive(false);
        }
        
        isFading = false;
        onComplete?.Invoke();
    }
    
    private IEnumerator FadeOutAndInCoroutine(System.Action onMidFade, System.Action onComplete)
    {
        isFading = true;
        
        // Fade out
        yield return StartCoroutine(FadeCoroutine(0f, 1f, null));
        
        // Execute mid-fade action (camera bound change)
        onMidFade?.Invoke();
        
        // Short pause while screen is black
        yield return new WaitForSecondsRealtime(0.1f);
        
        // Fade in
        yield return StartCoroutine(FadeCoroutine(1f, 0f, null));
        
        isFading = false;
        onComplete?.Invoke();
    }
    
    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
    }
    
    public bool IsFading()
    {
        return isFading;
    }
    
    // Quick fade methods
    public static void QuickFadeOut(System.Action onComplete = null)
    {
        Instance.FadeOut(onComplete);
    }
    
    public static void QuickFadeIn(System.Action onComplete = null)
    {
        Instance.FadeIn(onComplete);
    }
    
    public static void QuickFadeTransition(System.Action onMidFade = null, System.Action onComplete = null)
    {
        Instance.FadeOutAndIn(onMidFade, onComplete);
    }
}