using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UIElements;

public class Tile : MonoBehaviour
{
    public Sprite blackSprite;
    public Sprite whiteSprite;
    public Sprite WhiteAtkSprite;
    public Sprite BlackAtkSprite;
    public Sprite SelectedFrame;
    public Sprite AtkIcon;
    public Sprite MoveIcon;

    SpriteRenderer sr;
    SpriteRenderer IcnRenderer;
    public Color OnTileColor;
    public Color redColor;
    Color originalColor;

    public int X, Y;
    Board board;

    public bool is_selected = false;
    public bool is_legal_move = false;
    public bool is_attack_move = false;
    public bool is_left_castling = false;
    public bool is_right_castling = false;
    public bool is_enpassant = false;
    public bool isLastAttackedTile = false;

    bool is_white = false;

    void Awake()
    {
        board = Object.FindFirstObjectByType<Board>();
    }

    public void SetTile(bool isWhite)
    {
        is_white = isWhite;
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = is_white ? whiteSprite : blackSprite;
        originalColor = sr.color;

        GameObject statusIcn = new GameObject("StatusIcon");
        statusIcn.transform.SetParent(transform, false);
        IcnRenderer = statusIcn.AddComponent<SpriteRenderer>();
        IcnRenderer.sprite = null;
        IcnRenderer.sortingLayerName = "icon";
    }

    public void clearTile()
    {
        sr.sprite = is_white ? whiteSprite : blackSprite;
        IcnRenderer.sprite = null;

        is_selected = false;
        is_legal_move = false;
        is_attack_move = false;
        is_left_castling = false;
        is_right_castling = false;
        //is_enpassant = false;
        


        if (isLastAttackedTile) sr.color = redColor; 
        else sr.color = originalColor;

    }

    public void SetColor(Color color)
    {
        sr.color = color;
    }

    public void setLegalMove()
    {
        IcnRenderer.sprite = MoveIcon;
        is_legal_move = true;
    }

    public void setSelected()
    {
        IcnRenderer.sprite = SelectedFrame;
        is_selected = true;
    }

    public void setAttackMove()
    {
        clearTile();
        sr.sprite = is_white ? WhiteAtkSprite : BlackAtkSprite;
        IcnRenderer.sprite = AtkIcon;
        is_attack_move = true;
    }

    public void SetAsLastAttacked(bool state)
    {
        isLastAttackedTile = state;
        if (state)
        {
            sr.sprite = is_white ? whiteSprite : blackSprite;
            IcnRenderer.sprite = null;
            sr.color = redColor;
        }
        else clearTile();
    }


    void OnMouseEnter()
    {
        if (!board.isGameActive || (PromotionUI.Instance != null && PromotionUI.Instance.IsActive)) return;
        if (!is_selected && !is_attack_move && !is_legal_move && !isLastAttackedTile) SetColor(OnTileColor);
    }

    void OnMouseExit()
    {
        if (!board.isGameActive || (PromotionUI.Instance != null && PromotionUI.Instance.IsActive)) return;
        if (!is_selected && !is_attack_move && !is_legal_move && !isLastAttackedTile) SetColor(originalColor);
    }


    void OnMouseUpAsButton()
    {
        if (!board.isGameActive || (PromotionUI.Instance != null && PromotionUI.Instance.IsActive)) return;
        if (board.selectedPiece != null && board.selectedPiece.isMoving == true) return;
        board.OnTileClicked(X, Y);
    }

    void OnMouseOver()
    {
        if (!board.isGameActive || (PromotionUI.Instance != null && PromotionUI.Instance.IsActive)) return;

        if (Input.GetMouseButtonDown(1))
        {
            ChessPieces cp = board.pieces[X, Y];

            if (cp != null)
            {
                if (CardDescriptionUI.Instance != null) CardDescriptionUI.Instance.ShowPieceInfo(cp);
            }
            else
            {
                if (CardDescriptionUI.Instance != null) CardDescriptionUI.Instance.HidePieceInfo();
            }
        }
    }

}
