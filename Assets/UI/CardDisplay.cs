using UnityEngine;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    public CharacterCard characterCard;
    public Text characterNameText;
    public Image characterImage;

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (characterCard != null)
        {
            characterNameText.text = characterCard.characterName;
            characterImage.sprite = characterCard.characterSprite;
        }
    }
}
