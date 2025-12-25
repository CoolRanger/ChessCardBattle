using UnityEngine;
using UnityEngine.UI;

public class CardDescriptionUI : MonoBehaviour
{
    public static CardDescriptionUI Instance { get; private set; }

    [Header("UI 元件綁定")]
    public Image displayImage;

    [Header("邊距設定 (Stretch模式)")]
    public float topMargin = 200f;
    public float bottomMargin = 200f;
    public float marginLarge = 1225f; 
    public float marginSmall = 0f;    

    private RectTransform rectTransform;
    private Board board;

    void Awake()
    {
        Instance = this;

        if (displayImage != null)
        {
            rectTransform = displayImage.GetComponent<RectTransform>();
        }

        Hide();
    }

    void Start()
    {
        board = FindFirstObjectByType<Board>();
    }

    public void Show(CardData data)
    {
        if (data == null || displayImage == null) return;

        if (data.DescriptionArtwork != null) displayImage.sprite = data.DescriptionArtwork;

        UpdatePosition();

        displayImage.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (displayImage != null)
            displayImage.gameObject.SetActive(false);
    }

    private void UpdatePosition()
    {
        if (board == null) board = FindFirstObjectByType<Board>();
        if (rectTransform == null) return;

        bool isRightSide = false;

        if (board.currentTeam == -1) isRightSide = true;
        else isRightSide = (board.currentTeam == 0);


        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        if (isRightSide)
        {
            rectTransform.offsetMin = new Vector2(marginLarge, bottomMargin);
            rectTransform.offsetMax = new Vector2(-marginSmall, -topMargin);
        }
        else
        {
            rectTransform.offsetMin = new Vector2(marginSmall, bottomMargin);
            rectTransform.offsetMax = new Vector2(-marginLarge, -topMargin);
        }
    }
}