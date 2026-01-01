using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("¤¶­±ª«¥ó")]
    public GameObject settingsPanel;
    public GameObject creditsPanel;

    [Header("·Æ±ì")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    void Start()
    {
        if (AudioManager.Instance != null)
        {
            bgmSlider.value = AudioManager.Instance.GetBGMVolume();
            sfxSlider.value = AudioManager.Instance.GetSFXVolume();
        }

        bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);

        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }

    public void OnBGMChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetBGMVolume(value);
    }

    public void OnSFXChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
    }


    public void OpenSettings()
    {
        if (GameUI.Instance != null && GameUI.Instance.btnSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(GameUI.Instance.btnSound);
        }
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (GameUI.Instance != null && GameUI.Instance.btnSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(GameUI.Instance.btnSound);
        }
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }

    public void OpenCredits()
    {
        if (GameUI.Instance != null && GameUI.Instance.btnSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(GameUI.Instance.btnSound);
        }
        creditsPanel.SetActive(true);
    }
    public void CloseCredits()
    {
        if (GameUI.Instance != null && GameUI.Instance.btnSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(GameUI.Instance.btnSound);
        }
        creditsPanel.SetActive(false);
    }
}