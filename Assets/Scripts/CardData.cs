using UnityEngine;

[CreateAssetMenu(fileName = "New Card Data", menuName = "Card Game/New Card Data")]
public class CardData : ScriptableObject
{
    [Header("基本資訊")]
    public string cardName;
    public string displayName;
    public int cost;

    [Header("敘述與圖片")]
    public Sprite CardArtWork;
    public Sprite DescriptionArtwork;
}