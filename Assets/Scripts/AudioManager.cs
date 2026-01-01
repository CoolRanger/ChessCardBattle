using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        float savedBGM = PlayerPrefs.GetFloat("BGM_Vol", 0.5f);
        float savedSFX = PlayerPrefs.GetFloat("SFX_Vol", 0.5f);

        SetBGMVolume(savedBGM);
        SetSFXVolume(savedSFX);
    }

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource.clip == clip) return;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }


    public void SetBGMVolume(float value)
    {
        if (bgmSource != null) bgmSource.volume = value;
        PlayerPrefs.SetFloat("BGM_Vol", value);
    }

    public void SetSFXVolume(float value)
    {
        if (sfxSource != null) sfxSource.volume = value;
        PlayerPrefs.SetFloat("SFX_Vol", value);
    }

    public float GetBGMVolume() { return PlayerPrefs.GetFloat("BGM_Vol", 0.5f); }
    public float GetSFXVolume() { return PlayerPrefs.GetFloat("SFX_Vol", 0.5f); }
}