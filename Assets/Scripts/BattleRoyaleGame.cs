using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Linq;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class BattleRoyaleGame : MonoBehaviourPunCallbacks
{
    public static BattleRoyaleGame Instance;

    [Header("Managers & Spawners")]
    public CoinSpawner coinSpawner;
    public Scoreboard scoreboardManager;

    [Header("UI General")]
    public TMP_Text centralNotificationText; 
    public TMP_Text timerText;
    
    [Header("End Game UI Panel")]
    public GameObject endPanel;
    public TMP_Text endPanelTimerText;
    public Button skipButton;
    public TMP_Text rank1Text, rank2Text, rank3Text;

    private float matchTimer = 60f;
    private bool gameActive = false;
    private Coroutine returnRoutine;

    // Loading tracker
    private int playersLoadedInScene = 0;
    private bool sequenceStarted = false;

    // Tracks amount of players;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        endPanel.SetActive(false);
        skipButton.onClick.AddListener(SkipToEnd);

        // Reset local score
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { "Score", 0 } });

        timerText.text = "1:00";
        centralNotificationText.gameObject.SetActive(true);
        centralNotificationText.text = "Waiting...";

        // Tell Master Client that this player has loaded
        if (PhotonNetwork.IsConnectedAndReady)
        {
            photonView.RPC("RPC_PlayerLoaded", RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    private void RPC_PlayerLoaded()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        playersLoadedInScene++;

        if (playersLoadedInScene >= PhotonNetwork.CurrentRoom.PlayerCount && !sequenceStarted)
        {
            sequenceStarted = true;
            photonView.RPC("RPC_StartMatchCountdown", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_StartMatchCountdown()
    {
        StartCoroutine(StartMatchSequence());
    }

    private IEnumerator StartMatchSequence()
    {
        centralNotificationText.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            centralNotificationText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        centralNotificationText.text = "GO!";
        gameActive = true;

        if (PhotonNetwork.IsMasterClient && coinSpawner != null)
            coinSpawner.StartSpawning();

        yield return new WaitForSeconds(1f);
        centralNotificationText.gameObject.SetActive(false);
    }

    [System.Obsolete]
    private void Update()
    {
        if (!gameActive) return;

        if (GetAlivePlayerCount() <= 1)
        {
            StartCoroutine(EndMatchSequence());
        }

        matchTimer -= Time.deltaTime;
        if (matchTimer <= 0)
        {
            matchTimer = 0;
            StartCoroutine(EndMatchSequence());
        }


        timerText.text = $"0:{Mathf.CeilToInt(matchTimer):00}";
    }

    [System.Obsolete]
    private IEnumerator EndMatchSequence()
    {
        gameActive = false;
        if (PhotonNetwork.IsMasterClient && coinSpawner != null)
            coinSpawner.StopSpawning();

        centralNotificationText.gameObject.SetActive(true);
        centralNotificationText.text = "GAME OVER!";

        yield return new WaitForSeconds(1.5f);
        centralNotificationText.gameObject.SetActive(false);

        // ShowFinalResults();

        HighlightWinner();

        yield return new WaitForSeconds(3f);

        endPanel.SetActive(true);
        returnRoutine = StartCoroutine(ReturnToLobbyCountdown());
    }

    private void ShowFinalResults()
    {
        var sortedList = PhotonNetwork.PlayerList
            .OrderByDescending(p => GetPlayerScore(p))
            .ToList();

        rank1Text.text = sortedList.Count > 0 ? $"1st: {sortedList[0].NickName} ({GetPlayerScore(sortedList[0])} Coins)" : "";
        rank2Text.text = sortedList.Count > 1 ? $"2nd: {sortedList[1].NickName} ({GetPlayerScore(sortedList[1])} Coins)" : "";
        rank3Text.text = sortedList.Count > 2 ? $"3rd: {sortedList[2].NickName} ({GetPlayerScore(sortedList[2])} Coins)" : "";

        // Highlight winner
        if (sortedList.Count > 0)
        {
            HighlightWinner(sortedList[0]);
        }
    }

    private void HighlightWinner(Player winner)
    {
        PhotonView[] allViews = FindObjectsByType<PhotonView>(FindObjectsSortMode.None);
        foreach (var view in allViews)
        {
            if (view.Owner == winner && view.CompareTag("Player"))
            {
                Nametag tag = view.GetComponentInChildren<Nametag>();
                if (tag != null)
                    tag.ShowWinner();
            }
        }
    }

    [System.Obsolete]
    private void HighlightWinner()
    {
        var winnerController = FindObjectsOfType<PlayerController>()
            .Where(pc => pc.gameObject.CompareTag("Player") && pc.health > 0)
            .FirstOrDefault();

        if (winnerController == null)
            return;

        Nametag tag = winnerController.GetComponentInChildren<Nametag>();
        if (tag != null)
            tag.ShowWinner();
    }

    [System.Obsolete]
    private int GetAlivePlayerCount()
    {
        return FindObjectsOfType<PlayerController>()
            .Count(pc => pc.gameObject.CompareTag("Player") && pc.health > 0);
    }

    private int GetPlayerScore(Player p)
    {
        if (p.CustomProperties.TryGetValue("Score", out object s))
            return (int)s;
        return 0;
    }

    private IEnumerator ReturnToLobbyCountdown()
    {
        for (int i = 10; i > 0; i--)
        {
            endPanelTimerText.text = $"Returning to lobby in {i}s...";
            yield return new WaitForSeconds(1f);
        }
        PhotonNetwork.LeaveRoom();
    }

    public void SkipToEnd()
    {
        if (returnRoutine != null) StopCoroutine(returnRoutine);
        PhotonNetwork.LeaveRoom();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("Score") && scoreboardManager != null)
        {
            scoreboardManager.UpdateScoreboard();
        }
    }

    public override void OnLeftRoom()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("LandingPage");
    }
}