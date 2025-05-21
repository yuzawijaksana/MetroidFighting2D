using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "CharacterCard", menuName = "ScriptableObjects/CharacterCard")]
public class CharacterCard : ScriptableObject
{
    public string characterName;
    public Sprite characterSprite;
    public GameObject characterPrefab;


}
