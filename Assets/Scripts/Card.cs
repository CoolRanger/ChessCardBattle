using UnityEngine;

public class Card : MonoBehaviour
{
    // 卡牌資料
    public CardData data;

    public CardSystem owner;
    public Board board;

    private SpriteRenderer sr;
    private Color normalColor = Color.white;
    private Color selectedColor = new Color(0.7f, 0.9f, 1f);

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) Debug.LogError("Card Prefab 缺少 SpriteRenderer 元件！");

        board = Object.FindFirstObjectByType<Board>();
    }

    public void Init(CardData newData)
    {
        data = newData;
        if (sr != null && data != null && data.CardArtWork != null)
        {
            sr.sprite = data.CardArtWork;
            sr.color = normalColor;
        }
        else
        {
            Debug.LogWarning($"卡牌 {data?.name} 缺少手牌圖片 (CardArtWork) 或 SpriteRenderer");
        }
    }

    public void SetSelected(bool selected)
    {
        if (sr != null)
        {
            sr.color = selected ? selectedColor : normalColor;
        }
    }

    void OnMouseDown()
    {
        if (board == null || !board.isGameActive || (PromotionUI.Instance != null && PromotionUI.Instance.IsActive)) return;
        owner.SelectCard(this);
    }
}