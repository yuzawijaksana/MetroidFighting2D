using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CharacterIngameCellUI : MonoBehaviour
{
    [Header("References (Assign in Inspector)")]
    public Image maskImage; // The mask image to change color
    public Image artworkImage; // The character artwork image
    public Text nameText; // The character name text
    public Text damageText; // The damage percentage text (assign in inspector)

    [Header("Health UI")]
    public Transform heartsGrid;   // Assign your grid transform in inspector
    public Sprite heartSprite;     // Assign your heart sprite in inspector

    [HideInInspector]
    public CharacterCard characterCard;

    private List<Image> heartImages = new List<Image>();
    private Coroutine damageTextEffectCoroutine;
    private int defaultFontSize = 0;
    private float hitEffectScale = 1.3f;
    private float hitEffectDuration = 0.15f;

    // Call this to update the mask color (e.g., for health)
    public void SetMaskColor(Color color)
    {
        if (maskImage != null)
            maskImage.color = color;
    }

    // Call this to set the character info (sprite and name)
    public void SetCharacterInfo(Sprite sprite, string charName)
    {
        if (artworkImage != null)
            artworkImage.sprite = sprite;
        if (nameText != null)
            nameText.text = charName;
    }

    public void SetCharacterCard(CharacterCard card, string playerLabel = "")
    {
        characterCard = card;
        if (characterCard != null)
        {
            if (artworkImage != null)
                artworkImage.sprite = characterCard.characterSprite;
            if (nameText != null)
                nameText.text = characterCard.characterName; // Always show character name
        }
    }

    // Call this to update the mask color based on a Damageable component
    public void UpdateMaskColor(Damageable damageable)
    {
        if (damageable != null)
        {
            if (maskImage != null)
                maskImage.color = damageable.GetHealthColor();
            if (damageText != null)
            {
                damageText.text = Mathf.RoundToInt(damageable.currentHealth) + "%";
                DamageTextHitEffect();
            }
        }
    }

    private void DamageTextHitEffect()
    {
        if (damageText == null) return;
        if (defaultFontSize == 0)
            defaultFontSize = damageText.fontSize;
        if (damageTextEffectCoroutine != null)
            StopCoroutine(damageTextEffectCoroutine);
        damageTextEffectCoroutine = StartCoroutine(DamageTextHitEffectCoroutine());
    }

    private IEnumerator DamageTextHitEffectCoroutine()
    {
        float elapsed = 0f;
        float upTime = hitEffectDuration * 0.5f;
        float downTime = hitEffectDuration * 0.5f;
        int bigFont = Mathf.RoundToInt(defaultFontSize * hitEffectScale);

        // Scale up
        while (elapsed < upTime)
        {
            float t = elapsed / upTime;
            damageText.fontSize = Mathf.RoundToInt(Mathf.Lerp(defaultFontSize, bigFont, t));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        damageText.fontSize = bigFont;

        // Scale down
        elapsed = 0f;
        while (elapsed < downTime)
        {
            float t = elapsed / downTime;
            damageText.fontSize = Mathf.RoundToInt(Mathf.Lerp(bigFont, defaultFontSize, t));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        damageText.fontSize = defaultFontSize;
    }

    // Call this to initialize the hearts UI, now takes maxHearts from Damageable
    public void InitHearts(int maxHearts)
    {
        // Only create new hearts if not enough exist
        int toCreate = maxHearts - heartImages.Count;
        for (int i = 0; i < toCreate; i++)
        {
            GameObject heartGO = new GameObject("Heart", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            heartGO.transform.SetParent(heartsGrid, false);
            Image img = heartGO.GetComponent<Image>();
            img.sprite = heartSprite;
            img.SetNativeSize();
            heartImages.Add(img);
        }
        // Set all hearts active, then SetHearts will handle visibility
        for (int i = 0; i < heartImages.Count; i++)
        {
            heartImages[i].gameObject.SetActive(i < maxHearts);
        }
    }

    public void ClearHearts()
    {
        // Instead of destroying, just set all to inactive
        foreach (var img in heartImages)
        {
            if (img != null)
                img.gameObject.SetActive(false);
        }
    }

    // Call this to update the hearts display (e.g., after losing a life)
    public void SetHearts(int currentHearts)
    {
        for (int i = 0; i < heartImages.Count; i++)
        {
            heartImages[i].gameObject.SetActive(i < currentHearts);
        }
    }
}
