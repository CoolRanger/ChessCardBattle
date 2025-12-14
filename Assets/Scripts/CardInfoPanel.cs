using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class CardInfoPanel : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descText;

    CanvasGroup canvas;

    void Awake()
    {
        canvas = GetComponent<CanvasGroup>();
        Debug.Log(canvas == null ? "CanvasGroup MISSING" : "CanvasGroup OK");
        Hide();
    }


    public void Show(Card card)
    {
        //nameText.text = $"Name: {card.cardName}";
        nameText.text = $"Hi";
        costText.text = $"Cost: {card.cost}";
        descText.text = $"Description: {card.description}";

        canvas.alpha = 1;
        canvas.interactable = true;
        canvas.blocksRaycasts = true;
    }

    public void Hide()
    {
        canvas.alpha = 0;
        canvas.interactable = false;
        canvas.blocksRaycasts = false;
    }
}
