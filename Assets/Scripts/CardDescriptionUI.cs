using UnityEngine;
using UnityEngine.UI;
using TMPro; // 記得引用這個，因為我們要改文字

public class CardDescriptionUI : MonoBehaviour
{
    public static CardDescriptionUI Instance { get; private set; }

    [Header("卡牌 UI")]
    public Image displayImage;

    [Header("棋子 UI")]
    public GameObject pieceStatsRoot;  
    public TextMeshProUGUI pieceHpText;    
    public TextMeshProUGUI pieceAtkText; 
    public RectTransform pieceRect;

    [Header("遊戲資訊 UI")]
    public GameObject gameInfoRoot;
    public TextMeshProUGUI turnInfoText; 
    public TextMeshProUGUI skillLogText; 
    public RectTransform gameInfoRect;   
    public float gameInfoOffsetY = 300f;  

    [Header("邊距設定 (Stretch模式)")]
    public float topMargin = 200f;
    public float bottomMargin = 200f;
    public float marginLarge = 1225f;
    public float marginSmall = 0f;

    public float piecePanelOffsetY = -300f;

    private RectTransform cardRect; 
    private Board board;

    void Awake()
    {
        Instance = this;

        if (displayImage != null) cardRect = displayImage.GetComponent<RectTransform>();

        Hide();
        HidePieceInfo();
        if (gameInfoRoot != null) gameInfoRoot.SetActive(false);
        if (skillLogText != null) skillLogText.text = "";
    }

    void Start()
    {
        board = FindFirstObjectByType<Board>();
        UpdatePosition();
    }


    public void Show(CardData data)
    {
        if (data == null || displayImage == null) return;

        if (data.DescriptionArtwork != null)
            displayImage.sprite = data.DescriptionArtwork;

        UpdatePosition(); 
        displayImage.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (displayImage != null)
            displayImage.gameObject.SetActive(false);
    }

    public void ShowPieceInfo(ChessPieces piece)
    {
        if (piece == null || pieceStatsRoot == null) return;

        pieceHpText.text = $"HP: {piece.hp}";
        pieceAtkText.text = $"ATK: {piece.atk}";


        UpdatePosition();
        pieceStatsRoot.SetActive(true);
    }

    public void HidePieceInfo()
    {
        if (pieceStatsRoot != null)
            pieceStatsRoot.SetActive(false);
    }

    public void UpdateTurnText(bool isWhiteTurn, int currentStep)
    {
        if (turnInfoText == null) return;

        if (gameInfoRoot != null && !gameInfoRoot.activeSelf) gameInfoRoot.SetActive(true);

        string team = isWhiteTurn ? "White" : "Black";
        turnInfoText.text = $"{team}'s Turn Step: {currentStep + 1} / 2";

        UpdatePosition();
    }

    public void ShowSkillLog(bool isWhiteUser, string skillName)
    {
        if (skillLogText == null) return;

        string team = isWhiteUser ? "White" : "Black";
        skillLogText.text = $"{team} used {skillName}";

        UpdatePosition();
    }


    private void UpdatePosition()
    {
        if (board == null) board = FindFirstObjectByType<Board>();

        bool isRightSide = false;

        if (board.currentTeam == -1)
            isRightSide = true; 
        else
            isRightSide = (board.currentTeam == 0); 


        if (cardRect != null)
            ApplyStretchPosition(cardRect, isRightSide, 0);

        if (pieceRect != null)
            ApplyStretchPosition(pieceRect, isRightSide, piecePanelOffsetY);

        if (gameInfoRect != null)
            ApplyStretchPosition(gameInfoRect, isRightSide, gameInfoOffsetY);
    }

    private void ApplyStretchPosition(RectTransform rt, bool isRightSide, float offsetY)
    {
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);

        if (isRightSide)
        {
            rt.offsetMin = new Vector2(marginLarge, bottomMargin + offsetY);
            rt.offsetMax = new Vector2(-marginSmall, -topMargin + offsetY);
        }
        else
        {
            rt.offsetMin = new Vector2(marginSmall, bottomMargin + offsetY);
            rt.offsetMax = new Vector2(-marginLarge, -topMargin + offsetY);
        }
    }
}