using UnityEngine;
using UnityEngine.UI;

public class EnergyBar : MonoBehaviour
{
    public int energy = 0;
    public Sprite[] energySprites; // size = 11
    private SpriteRenderer sr;

    public void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }
    public void SetEnergy(int value)
    {
        energy = Mathf.Clamp(value, 0, 10);
        sr.sprite = energySprites[energy];
    }

    public void AddEnergy(int value)
    {
        if(energy + value > 10)
        {
            Debug.Log("Energy is full!");
            return;
        }
        SetEnergy(energy + value);
    }

    public void MinusEnergy(int value)
    {
        if(value > energy)
        {
            Debug.Log("Not enough energy!");
            return;
        }
        SetEnergy(energy - value);
    }

}
