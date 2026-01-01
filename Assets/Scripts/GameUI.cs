using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; set; }

    public Server server;
    public Client client;
    public Board board;

    [SerializeField] private Animator menuAnimator;
    [SerializeField] private TMP_InputField addressInput;

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI winnerText;

    public GameObject settingsButtonObj;

    public GameObject gameInfoRoot;

    public AudioClip menuBGM;
    public AudioClip btnSound;

    public Transform cameraRig;

    private void Awake()
    {
        Instance = this;
    }

    private void PlayButtonSound()
    {
        if (AudioManager.Instance != null && btnSound != null)
        {
            AudioManager.Instance.PlaySFX(btnSound);
        }
    }

    public void OnLocalGameButton()
    {
        PlayButtonSound();
        menuAnimator.SetTrigger("InGameMenu");

        gameInfoRoot.SetActive(true);
        if (settingsButtonObj != null) settingsButtonObj.SetActive(false);

        if (SkipButtonUI.Instance != null) SkipButtonUI.Instance.InitializeGameUI();


        board.StartLocalGame();

        if (cameraRig != null)
        {
            cameraRig.position = new Vector3(0, 15, -10);
            cameraRig.rotation = Quaternion.identity;

            StartCoroutine(MoveCameraToBoard(new Vector3(0, 0, -10), Quaternion.identity, 0f));
        }

        StartCoroutine(MoveCameraToBoard(new Vector3(0, 0, -10), Quaternion.identity, 0f));
    }

    public void OnOnlineGameStart()
    {
        menuAnimator.SetTrigger("InGameMenu");
        gameInfoRoot.SetActive(true);
        if (settingsButtonObj != null) settingsButtonObj.SetActive(false);

        if (SkipButtonUI.Instance != null) SkipButtonUI.Instance.InitializeGameUI();

        if (cameraRig != null)
        {
            cameraRig.position = new Vector3(0, 15, -10);
            cameraRig.rotation = Quaternion.identity;
        }

        Quaternion targetRot = Quaternion.identity;
        if (board.currentTeam == 1)
        {
            targetRot = Quaternion.Euler(0, 0, 180);
        }

        StartCoroutine(MoveCameraToBoard(new Vector3(0, 0, -10), targetRot, 0f));
    }

    private IEnumerator MoveCameraToBoard(Vector3 targetPos, Quaternion targetRot, float duration)
    {
        if (cameraRig == null) yield break; 

        Vector3 startPos = cameraRig.position; 
        Quaternion startRot = cameraRig.rotation;
        float time = 0;

        while (time < duration)
        {
            float t = time / duration;
            t = t * t * t * (t * (6f * t - 15f) + 10f);

            cameraRig.position = Vector3.Lerp(startPos, targetPos, t);
            cameraRig.rotation = Quaternion.Lerp(startRot, targetRot, t);

            time += Time.deltaTime;
            yield return null;
        }

        cameraRig.position = targetPos;
        cameraRig.rotation = targetRot;
    }

    public void OnOnlineGameButton()
    {
        PlayButtonSound();
        if (settingsButtonObj != null) settingsButtonObj.SetActive(false);
        menuAnimator.SetTrigger("OnlineMenu");
    }

    public void OnOnlineHostButton()
    {
        PlayButtonSound();
        server.Init(8007);
        client.Init("127.0.0.1", 8007);
        menuAnimator.SetTrigger("HostMenu");
    }

    public void OnOnlineConnectButton()
    {
        PlayButtonSound();
        string ip = addressInput.text;
        if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";
        client.Init(ip, 8007);
    }

    public void OnOnlineBackButton()
    {
        PlayButtonSound();
        if (settingsButtonObj != null) settingsButtonObj.SetActive(true);
        menuAnimator.SetTrigger("StartMenu");
    }

    public void OnHostBackButton()
    {
        PlayButtonSound();
        server.ShutDown();
        client.ShutDown();
        menuAnimator.SetTrigger("OnlineMenu");
    }

    public void OnGameWon(int winner)
    {
        board.isGameActive = false;
        gameInfoRoot.SetActive(false);
        gameOverPanel.SetActive(true);
        if (CardDescriptionUI.Instance != null)
        {
            CardDescriptionUI.Instance.Hide();         
            CardDescriptionUI.Instance.HidePieceInfo();
        }
        CardDescriptionUI.Instance.pieceStatsRoot.SetActive(false);
        winnerText.text = (winner == 0) ? "White Wins!" : "Black Wins!";
    }

    public void OnRestartButton()
    {
        PlayButtonSound();
        gameOverPanel.SetActive(false);
        board.StartLocalGame();

        OnLocalGameButton();
    }

    public void OnMenuButton()
    {
        PlayButtonSound();
        if (AudioManager.Instance != null && menuBGM != null) AudioManager.Instance.PlayBGM(menuBGM);
        gameOverPanel.SetActive(false);
        gameInfoRoot.SetActive(false);
        if (settingsButtonObj != null) settingsButtonObj.SetActive(true);
        if (SkipButtonUI.Instance != null) SkipButtonUI.Instance.ResetState();
        menuAnimator.SetTrigger("StartMenu");

        server.ShutDown();
        client.ShutDown();

        if (CardDescriptionUI.Instance != null)
        {
            CardDescriptionUI.Instance.Hide();      
            CardDescriptionUI.Instance.HidePieceInfo(); 
        }

        if (cameraRig != null)
        {
            StartCoroutine(MoveCameraToBoard(new Vector3(0, 15, -10), Quaternion.identity, 0f));
        }
    }

    public void OnsurrenderButton()
    {

        if (!board.isGameActive) return;
        PlayButtonSound();
        if (board.currentTeam != -1)
        {
            NetSurrender ns = new NetSurrender();
            ns.teamId = board.currentTeam;
            client.SendToServer(ns);
        }
        else
        {
            int winner = board.isWhiteTurn ? 1 : 0;
            OnGameWon(winner);
        }
    }
}