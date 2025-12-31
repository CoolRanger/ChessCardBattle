using System.Collections;
using TMPro;
using UnityEngine;

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

    public GameObject gameInfoRoot;

    public AudioClip menuBGM;

    private void Awake()
    {
        Instance = this;
    }

    public void OnLocalGameButton()
    {
        menuAnimator.SetTrigger("InGameMenu");
        board.StartLocalGame();
        
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(0, 15, -10);
            Camera.main.transform.rotation = Quaternion.identity;
        }

        StartCoroutine(MoveCameraToBoard(new Vector3(0, 0, -10), Quaternion.identity, 1.5f));
    }

    public void OnOnlineGameStart()
    {
        menuAnimator.SetTrigger("InGameMenu");

        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(0, 15, -10);
            Camera.main.transform.rotation = Quaternion.identity;
        }

        Quaternion targetRot = Quaternion.identity;
        if (board.currentTeam == 1)
        {
            targetRot = Quaternion.Euler(0, 0, 180);
        }

        StartCoroutine(MoveCameraToBoard(new Vector3(0, 0, -10), targetRot, 1.5f));
    }

    private IEnumerator MoveCameraToBoard(Vector3 targetPos, Quaternion targetRot, float duration)
    {
        if (Camera.main == null) yield break;

        Transform camTransform = Camera.main.transform;
        Vector3 startPos = camTransform.position;
        Quaternion startRot = camTransform.rotation; 
        float time = 0;

        while (time < duration)
        {
            float t = time / duration;
            t = t * t * t * (t * (6f * t - 15f) + 10f);

            camTransform.position = Vector3.Lerp(startPos, targetPos, t);
            camTransform.rotation = Quaternion.Lerp(startRot, targetRot, t);

            time += Time.deltaTime;
            yield return null;
        }

        camTransform.position = targetPos;
        camTransform.rotation = targetRot;
    }

    public void OnOnlineGameButton()
    {
        menuAnimator.SetTrigger("OnlineMenu");
    }

    public void OnOnlineHostButton()
    {
        server.Init(8007);
        client.Init("127.0.0.1", 8007);
        menuAnimator.SetTrigger("HostMenu");
    }

    public void OnOnlineConnectButton()
    {
        string ip = addressInput.text;
        if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";
        client.Init(ip, 8007);
    }

    public void OnOnlineBackButton()
    {
        menuAnimator.SetTrigger("StartMenu");
    }

    public void OnHostBackButton()
    {
        server.ShutDown();
        client.ShutDown();
        menuAnimator.SetTrigger("OnlineMenu");
    }

    public void OnGameWon(int winner)
    {
        board.isGameActive = false;
        gameOverPanel.SetActive(true);
        winnerText.text = (winner == 0) ? "White Wins!" : "Black Wins!";
    }

    public void OnRestartButton()
    {
        gameOverPanel.SetActive(false);
        board.StartLocalGame();

        OnLocalGameButton();
    }

    public void OnMenuButton()
    {
        if (AudioManager.Instance != null && menuBGM != null) AudioManager.Instance.PlayBGM(menuBGM);
        gameOverPanel.SetActive(false);
        gameInfoRoot.SetActive(false);
        menuAnimator.SetTrigger("StartMenu");

        server.ShutDown();
        client.ShutDown();

        if (CardDescriptionUI.Instance != null)
        {
            CardDescriptionUI.Instance.Hide();      
            CardDescriptionUI.Instance.HidePieceInfo(); 
        }

        if (Camera.main != null)
        {
            StartCoroutine(MoveCameraToBoard(new Vector3(0, 15, -10), Quaternion.identity, 1.0f));
        }
    }
}