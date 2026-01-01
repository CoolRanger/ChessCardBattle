using UnityEngine;
using UnityEngine.UI;

public class SkipButtonUI : MonoBehaviour
{
    public static SkipButtonUI Instance { get; private set; }

    [Header("按鈕物件")]
    public Button skipBtn;
    public Button surrenderBtn;

    [Header("定位點")]
    public RectTransform posWhiteSkip;  
    public RectTransform posBlackSkip;  
    public RectTransform posWhiteSurrender; 
    public RectTransform posBlackSurrender; 

    private Board board;

    private void Awake()
    {
        Instance = this;
        board = FindFirstObjectByType<Board>();
    }

    void Start()
    {
        if (board == null) Debug.LogError("SkipButtonUI 找不到 Board！");

        skipBtn.onClick.AddListener(() => board.OnSkipButtonUser());
        surrenderBtn.onClick.AddListener(() => GameUI.Instance.OnsurrenderButton());
    }

    public void InitializeGameUI()
    {
        UpdatePosition();
        ShowButtons();
    }

    public void ResetState()
    {
        HideButtons();
    }

    private void HideButtons()
    {
        skipBtn.gameObject.SetActive(false);
        surrenderBtn.gameObject.SetActive(false);
    }

    private void ShowButtons()
    {
        skipBtn.gameObject.SetActive(true);
        surrenderBtn.gameObject.SetActive(true);
    }

    void UpdatePosition()
    {
        if (board == null) return;

        RectTransform targetSkipPos;
        RectTransform targetSurrenderPos;

        if (board.currentTeam == 1)
        {
            targetSkipPos = posBlackSkip;
            targetSurrenderPos = posBlackSurrender;
        }
        else
        {
            targetSkipPos = posWhiteSkip;
            targetSurrenderPos = posWhiteSurrender;
        }

        if (targetSkipPos != null)
        {
            RectTransform skipRect = skipBtn.GetComponent<RectTransform>();
            SnapTo(skipRect, targetSkipPos);
        }

        if (targetSurrenderPos != null)
        {
            RectTransform surrenderRect = surrenderBtn.GetComponent<RectTransform>();
            SnapTo(surrenderRect, targetSurrenderPos);
        }
    }

    void SnapTo(RectTransform source, RectTransform target)
    {
        source.anchorMin = target.anchorMin;
        source.anchorMax = target.anchorMax;
        source.pivot = target.pivot;

        source.anchoredPosition = target.anchoredPosition;


        source.localScale = Vector3.one;

        Vector3 pos = source.localPosition;
        pos.z = 0;
        source.localPosition = pos;
    }

    void Update()
    {
        if (board == null || !board.isGameActive) return;

        UpdatePosition();

        bool canInteract = false;
        if (board.currentTeam == -1) canInteract = true;
        else
        {
            bool isMyTurn = (board.currentTeam == 0 && board.isWhiteTurn) ||
                            (board.currentTeam == 1 && !board.isWhiteTurn);
            canInteract = isMyTurn;
        }
        skipBtn.interactable = canInteract;
    }
}