using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CharacterIngameCellUI : MonoBehaviour
{
    [Header("References (Assign in Inspector)")]
    public Image maskImage; // The mask image to change color
    public Image artworkImage; // The character artwork image
    public Text nameText; // The character name text

    [Header("Health UI")]
    public Transform heartsGrid;   // Assign your grid transform in inspector
    public Sprite heartSprite;     // Assign your heart sprite in inspector

    [HideInInspector]
    public CharacterCard characterCard;

    private List<Image> heartImages = new List<Image>();

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
                nameText.text = playerLabel;
        }
    }

    // Call this to update the mask color based on a Damageable component
    public void UpdateMaskColor(Damageable damageable)
    {
        if (damageable != null && maskImage != null)
        {
            maskImage.color = damageable.GetHealthColor();
        }
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
