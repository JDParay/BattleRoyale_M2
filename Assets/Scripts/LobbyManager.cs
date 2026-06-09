using UnityEngine;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Debug Mode")]
    [Tooltip("Turn this on for specific debug purposes only. Turn off when not needed.")]
    public bool DebugMode = false;
    [Tooltip("Referenced amount of minimum players.")]
    public int minimumPlayers;

    [Header("UI References")]
    public TMP_Text lobbyTitleText;
    public TMP_Text playerCountText;
    public Button actionButton;
    public TMP_Text actionButtonText;
    
    [Header("Player List Setup")]
    public GameObject playerEntryPrefab; 
    public Transform contentPanel;
    public TMP_InputField maxPlayersInput;       
    private Dictionary<int, GameObject> playerListEntries = new Dictionary<int, GameObject>();

    [Header("Game Mode Settings")]
    public TMP_Dropdown gameModeDropdown;
    public TMP_Text selectedModeDisplayText;
    private GameMode currentGameMode = GameMode.CoinCollect;

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (DebugMode)
        {
            minimumPlayers = 1;
        } else
        {
            minimumPlayers = 3;
        }
    }
    private void Start()
    {

        RefreshPlayerList();
        UpdateLobbyUI();

        SetupGameModeDropdown();
    }

    private void RefreshPlayerList()
    {
        foreach (GameObject obj in playerListEntries.Values) Destroy(obj);
        playerListEntries.Clear();

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            AddPlayerToList(p);
        }
    }

    private void SetupGameModeDropdown()
    {
        if (gameModeDropdown == null) return;

        gameModeDropdown.ClearOptions();
        gameModeDropdown.AddOptions(new List<string> { "Coin Collect", "Battle Royale" });

        gameModeDropdown.onValueChanged.AddListener(OnGameModeChanged);

        // Only Master Client can change the mode
        gameModeDropdown.interactable = PhotonNetwork.IsMasterClient;

        // Set initial value
        gameModeDropdown.value = (int)currentGameMode;
    }

    // --- READY SYSTEM LOGIC ---

    public void OnClickActionButton()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Save the current game mode to room properties
            Hashtable props = new Hashtable { { "GameMode", (byte)currentGameMode } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            string sceneName = currentGameMode.ToString() + "Lobby";
            
            Debug.Log($"Loading 3D Lobby: {sceneName}");
            PhotonNetwork.LoadLevel(sceneName);
        }
        else
        {

            bool isReady = false;
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("IsReady", out object ready))
            {
                isReady = (bool)ready;
            }

            Hashtable props = new Hashtable { { "IsReady", !isReady } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("IsReady"))
        {
            if (playerListEntries.TryGetValue(targetPlayer.ActorNumber, out GameObject entry))
            {
                UpdateEntryDisplay(entry, targetPlayer);
            }
            UpdateLobbyUI();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"New player joined: {newPlayer.NickName}");

        RefreshPlayerList();
        
        UpdateLobbyUI();
    }

        public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (playerListEntries.ContainsKey(otherPlayer.ActorNumber))
        {
            Destroy(playerListEntries[otherPlayer.ActorNumber]);
            playerListEntries.Remove(otherPlayer.ActorNumber);
        }

        UpdateLobbyUI(); 
    }

    private void UpdateEntryDisplay(GameObject entry, Player player)
    {
        TMP_Text nameText = entry.transform.Find("NameText").GetComponent<TMP_Text>();
        TMP_Text statusText = entry.transform.Find("StatusText").GetComponent<TMP_Text>();

        nameText.text = player.NickName;

        if (player.IsMasterClient)
        {
            statusText.text = "<color=yellow><b>Host</b></color>";
        }
        else
        {
            bool isReady = false;
            if (player.CustomProperties.TryGetValue("IsReady", out object ready))
            {
                isReady = (bool)ready;
            }

            statusText.text = isReady ? "<color=green>Ready</color>" : "<color=red>Not Ready</color>";
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"New Master Client: {newMasterClient.NickName}");
        
        RefreshPlayerList();
        UpdateLobbyUI();
    }

    // --- EXISTING LOGIC ---

    private void UpdateLobbyUI()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        playerCountText.text = $"{currentPlayers} / {PhotonNetwork.CurrentRoom.MaxPlayers}";

        if (maxPlayersInput != null)
        {
            maxPlayersInput.text = PhotonNetwork.CurrentRoom.MaxPlayers.ToString();
            maxPlayersInput.interactable = PhotonNetwork.IsMasterClient;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            actionButtonText.text = "Start Game";
            actionButton.interactable = CheckIfEveryoneIsReady() && currentPlayers >= minimumPlayers;
        }
        else
        {
            bool isReady = false;
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("IsReady", out object ready))
                isReady = (bool)ready;

            actionButtonText.text = isReady ? "Unready" : "Ready";
            actionButton.interactable = true;
        }
    }

        private bool CheckIfEveryoneIsReady()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.IsMasterClient) continue;

            if (p.CustomProperties.TryGetValue("IsReady", out object ready))
            {
                if (!(bool)ready) return false;
            }
            else 
            {
                return false;
            }
        }
        return true;
    }

public override void OnJoinedRoom()
{
    Debug.Log($"OnJoinedRoom() -> IsMasterClient: {PhotonNetwork.IsMasterClient}");

    if (PhotonNetwork.IsMasterClient)
    {
        StartCoroutine(DelayedLobbySetup(0.4f)); 
    }
    else
    {
        StartCoroutine(DelayedLobbySetup(0.15f));
    }
}

private IEnumerator DelayedLobbySetup(float delay)
{
    yield return new WaitForSeconds(delay);

    // === LOBBY TITLE ===
    if (lobbyTitleText != null && PhotonNetwork.CurrentRoom != null)
    {
        string title = "Lobby #" + PhotonNetwork.CurrentRoom.Name;
        lobbyTitleText.text = title;
        lobbyTitleText.SetAllDirty();
        Debug.Log($"✅ Lobby Title Updated: {title} | IsMaster: {PhotonNetwork.IsMasterClient}");
    }
    else
    {
        Debug.LogWarning("Lobby title or room still not ready...");
    }

    // Normal setup
    Hashtable props = new Hashtable { { "IsReady", false } };
    PhotonNetwork.LocalPlayer.SetCustomProperties(props);

    foreach (GameObject obj in playerListEntries.Values) Destroy(obj);
    playerListEntries.Clear();

    RefreshPlayerList();
    UpdateLobbyUI();

    maxPlayersInput.interactable = PhotonNetwork.IsMasterClient;

    SetupGameModeDropdown();

    // Load saved game mode
    if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("GameMode", out object modeObj))
    {
        currentGameMode = (GameMode)System.Convert.ToByte(modeObj);
        if (gameModeDropdown != null)
            gameModeDropdown.value = (int)currentGameMode;
    }
}

    private void OnGameModeChanged(int index)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        currentGameMode = (GameMode)index;
        
        Hashtable roomProps = new Hashtable { { "GameMode", (byte)currentGameMode } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
    }
    private void AddPlayerToList(Player player)
    {
        GameObject entry = Instantiate(playerEntryPrefab, contentPanel);
        UpdateEntryDisplay(entry, player);
        playerListEntries.Add(player.ActorNumber, entry);
    }
    
    public void SetMaxPlayersFromInput(string input)
    {
        if (!PhotonNetwork.IsMasterClient) 
        {
            if (maxPlayersInput != null && PhotonNetwork.CurrentRoom != null)
                maxPlayersInput.text = PhotonNetwork.CurrentRoom.MaxPlayers.ToString();
            return;
        }

        if (int.TryParse(input, out int newMax))
        {
            // IF IN DEBUG MODE, minimum players becomes 2 (so dev only has to run one other clone to test.) IF NOT, minPlayers = 3.
            // int minimumPlayers;
            // if (DebugMode)
            // {
            //     minimumPlayers = 2;
            // } else
            // {
            //     minimumPlayers = 3;
            // }
            byte clampedMax = (byte)Mathf.Clamp(newMax, minimumPlayers, 12);
            
            PhotonNetwork.CurrentRoom.MaxPlayers = clampedMax;

            UpdateLobbyUI();

            Debug.Log($"Max Players changed to: {clampedMax}");
        }
    }
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("GameMode"))
        {
            currentGameMode = (GameMode)(byte)propertiesThatChanged["GameMode"];

            if (gameModeDropdown != null)
                gameModeDropdown.value = (int)currentGameMode;

            if (selectedModeDisplayText != null)
            {
                selectedModeDisplayText.text = currentGameMode == GameMode.CoinCollect 
                    ? "Coin Collect" 
                    : "Battle Royale";
            }
        }

        // Extra title safety
        if (lobbyTitleText != null && PhotonNetwork.CurrentRoom != null)
        {
            lobbyTitleText.text = "Lobby #" + PhotonNetwork.CurrentRoom.Name;
        }

        UpdateLobbyUI();
    }
        public void OnClickLeaveLobby() => PhotonNetwork.LeaveRoom();
        public override void OnLeftRoom() => UnityEngine.SceneManagement.SceneManager.LoadScene("LandingPage");
    }