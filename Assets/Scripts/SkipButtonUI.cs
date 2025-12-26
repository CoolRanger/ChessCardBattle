using UnityEngine;
using UnityEngine.UI;

public class SkipButtonUI : MonoBehaviour
{
    public Button myButton;
    public RectTransform rectTransform;

    [Header("位置設定")]
    public float marginX = 50f;
    public float marginY = 50f;

    [Header("延遲顯示")]
    public float delayTime = 1.0f; 
    private float currentTimer = 0f;

    private Board board;

    void Start()
    {
        board = FindFirstObjectByType<Board>();
        if (myButton == null) myButton = GetComponent<Button>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        myButton.onClick.AddListener(() => board.OnSkipButtonUser());
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        if (board == null) return;

        if (!board.isGameActive)
        {
            currentTimer = 0f;
            transform.localScale = Vector3.zero; 
            return;
        }

        if (currentTimer < delayTime)
        {
            currentTimer += Time.deltaTime;
            transform.localScale = Vector3.zero; 
            return; 
        }

        transform.localScale = Vector3.one;
        UpdatePosition();
        UpdateInteractable();
    }

    void UpdateInteractable()
    {
        bool canInteract = false;

        if (board.currentTeam == -1)
        {
            canInteract = true;
        }
        else
        {
            bool isMyTurn = (board.currentTeam == 0 && board.isWhiteTurn) ||
                            (board.currentTeam == 1 && !board.isWhiteTurn);
            canInteract = isMyTurn;
        }

        myButton.interactable = canInteract;
    }

    void UpdatePosition()
    {
        bool isRightSide = false;

        if (board.currentTeam == -1)
            isRightSide = true; 
        else
            isRightSide = (board.currentTeam == 0); 

        rectTransform.anchorMin = new Vector2(isRightSide ? 1 : 0, 0);
        rectTransform.anchorMax = new Vector2(isRightSide ? 1 : 0, 0);
        rectTransform.pivot = new Vector2(isRightSide ? 1 : 0, 0); 

        if (isRightSide) rectTransform.anchoredPosition = new Vector2(-marginX, marginY);
        else rectTransform.anchoredPosition = new Vector2(marginX, marginY);
    }
}