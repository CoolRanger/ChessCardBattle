using UnityEngine;
using UnityEngine.UI;
using System;

public class PromotionUI : MonoBehaviour
{
    public static PromotionUI Instance;

    [Header("UI Components")]
    public GameObject panel;

    [Header("Buttons")]
    public Button queenBtn;
    public Button rookBtn;
    public Button bishopBtn;
    public Button knightBtn;

    [Header("Backgrounds")]
    public Sprite whitePanelBg;
    public Sprite blackPanelBg;

    [Header("Icons Settings")]
    //order: queen, rook, bishop, knight
    public Sprite[] whiteSprites;
    public Sprite[] blackSprites;

    private Action<string> onPieceSelected;

    void Awake()
    {
        Instance = this;

        if (panel != null) panel.SetActive(false);
        else gameObject.SetActive(false);

        queenBtn.onClick.AddListener(() => SelectPiece("queen"));
        rookBtn.onClick.AddListener(() => SelectPiece("rook"));
        bishopBtn.onClick.AddListener(() => SelectPiece("bishop"));
        knightBtn.onClick.AddListener(() => SelectPiece("knight"));

        panel.SetActive(false);
    }

    public bool IsActive
    {
        get { return panel.activeSelf; }
    }

    public void Show(string team, Action<string> callback)
    {
        onPieceSelected = callback;

        Image panelImg = panel.GetComponent<Image>();
        if (panelImg != null)
        {
            if (team == "white") panelImg.sprite = whitePanelBg;
            else panelImg.sprite = blackPanelBg;
        }

        panelImg.color = new Color(256, 256, 256, 256);

        Sprite[] targetSprites = (team == "white") ? whiteSprites : blackSprites;

        queenBtn.GetComponent<Image>().sprite = targetSprites[0];
        rookBtn.GetComponent<Image>().sprite = targetSprites[1];
        bishopBtn.GetComponent<Image>().sprite = targetSprites[2];
        knightBtn.GetComponent<Image>().sprite = targetSprites[3];

        panel.SetActive(true);
    }

    private void SelectPiece(string type)
    {
        panel.SetActive(false);
        onPieceSelected?.Invoke(type);
    }


}