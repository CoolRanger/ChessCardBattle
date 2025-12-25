using UnityEngine;

[CreateAssetMenu(fileName = "New Card Data", menuName = "Card Game/New Card Data")]
public class CardData : ScriptableObject
{
    [Header("基本資訊")]
    public string cardName;      
    public int cost;       

    [Header("敘述與圖片")]
    [TextArea(5, 10)] 
    public Sprite DescriptionArtwork;
    public Sprite CardArtWork;

}