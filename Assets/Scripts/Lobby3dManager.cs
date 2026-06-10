using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Lobby3DManager : MonoBehaviourPunCallbacks
{
    public static Lobby3DManager Instance;

    [Header("Environments")]
    public GameObject lobbyEnvironment;
    public GameObject minigameEnvironment;

    [Header("Name Change UI")]
    public GameObject nameChangePanel;
    public TMP_InputField nameInputField;

    [Header("Color Picker")]
    public GameObject colorChangePanel;
    private ChangeColor localPlayerColor; 

    private PhotonView pv;

    private Camera mainCam;
    private Camera lobbyCam;

        private readonly Dictionary<string, Color> namedColors = new Dictionary<string, Color>()
    {
        { "Red",    Color.red },
        { "Blue",   Color.blue },
        { "Green",  Color.green },
        { "Yellow", Color.yellow },
        { "Orange", new Color(1f, 0.5f, 0f) },
        { "White",  Color.white },
    };

    private void Awake()
    {
        Instance = this;
        pv = GetComponent<PhotonView>();
        if (pv == null)
        {
            pv = gameObject.AddComponent<PhotonView>();
            Debug.LogWarning("✅ Added missing PhotonView to Lobby3DManager");
        }

        mainCam = Camera.main;
        // Find lobby camera (you can assign it via inspector too)
        lobbyCam = GameObject.Find("LobbyCamera")?.GetComponent<Camera>();

        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        SetReady(false);
        SetupColorButtons();
    }

    // ====================== NAME CHANGE ======================

    private void SetupColorButtons()
{
    if (colorChangePanel == null) return;

    foreach (Button btn in colorChangePanel.GetComponentsInChildren<Button>())
    {
        string btnName = btn.gameObject.name;

        if (namedColors.TryGetValue(btnName, out Color color))
        {
            // Tint the button itself so it looks like a swatch
            btn.GetComponent<Image>().color = color;

            // Capture for closure
            Color captured = color;
            btn.onClick.AddListener(() => localPlayerColor?.ApplyColor(captured));
        }
        else
        {
            Debug.LogWarning($"Color button '{btnName}' has no matching color defined.");
        }
    }
}
    public void RegisterLocalPlayer(GameObject player)
    {
        localPlayerColor = player.GetComponent<ChangeColor>();
    }
    
    public void ShowNameChangeUI()
    {
        if (nameChangePanel == null || nameInputField == null) return;

        nameInputField.text = PhotonNetwork.NickName;
        nameChangePanel.SetActive(true);
        colorChangePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        nameInputField.ActivateInputField();
    }

    public void HideNameChangeUI()
    {
        if (nameChangePanel != null) nameChangePanel.SetActive(false);
    }

    public void OpenColorChangeUI()
    {
        if (nameChangePanel != null) nameChangePanel.SetActive(false);
        colorChangePanel.SetActive(true);

    }

    public void SetEditingStatus(bool isEditing)
    {
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { "IsEditing", isEditing } });
    }

    public void ConfirmNameChange()
    {
        if (string.IsNullOrWhiteSpace(nameInputField.text))
        {
            Debug.LogWarning("Name cannot be empty!");
            return;
        }

        string newName = nameInputField.text.Trim();

        if (newName.Length > 12)
            newName = newName.Substring(0, 12);

        PhotonNetwork.NickName = newName;

        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable 
        { 
            { "NickName", newName } 
        });

        HideNameChangeUI();

        Debug.Log("Name changed to: " + newName);
    }


    // ====================== READY ======================
    public void SetReady(bool ready)
    {
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { "IsReady", ready } });
    }

    public void ToggleLocalReady()
    {
        bool current = false;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("IsReady", out object r) && r is bool b)
            current = b;

        SetReady(!current);
    }

    public void StartLeaveProcess()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("LandingPage");
    }

    // ====================== SAFE READY CHECK ======================
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!changedProps.ContainsKey("IsReady") || !PhotonNetwork.IsMasterClient)
            return;

        Invoke(nameof(CheckIfAllReadySafe), 0.3f);

            if (lobbyCam != null)
        {
            mainCam.enabled = false;
            lobbyCam.enabled = true;
        }

            foreach (var pv in FindObjectsOfType<PhotonView>())
        {
            if (pv.Owner == targetPlayer)
            {
                pv.GetComponent<ChangeColor>()?.ApplyFromPhotonProperties(changedProps);
                break;
            }
        }
    }

    private void CheckIfAllReadySafe()
    {
        try
        {
            if (PhotonNetwork.CurrentRoom == null) return;

            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
            if (playerCount <= 0) return;

            int readyCount = 0;

            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p == null) continue;

                bool isReady = false;
                if (p.CustomProperties != null && 
                    p.CustomProperties.TryGetValue("IsReady", out object obj) && obj is bool b)
                {
                    isReady = b;
                }

                if (isReady) readyCount++;
            }

            if (readyCount >= playerCount)
            {
                Debug.Log("✅ ALL PLAYERS READY - Sending RPC!");
                if (pv != null)
                    pv.RPC("RPC_TransitionToMinigame", RpcTarget.All);
                else
                    Debug.LogError("❌ No PhotonView on Lobby3DManager!");
            }
        }
        catch 
        {
            // Silent during joins
        }
    }

    public void OnLobbyExit()
    {
        if (mainCam != null) mainCam.enabled = true;
        if (lobbyCam != null) lobbyCam.enabled = false;
    }

    [PunRPC]
    private void RPC_TransitionToMinigame()
    {
        Debug.Log("🔄 RPC_TransitionToMinigame RECEIVED! Switching environments...");

        if (lobbyEnvironment != null) lobbyEnvironment.SetActive(false);
        if (minigameEnvironment != null) minigameEnvironment.SetActive(true);

        var spawner = FindFirstObjectByType<GameplaySpawner>();
        if (spawner != null)
            spawner.MoveExistingPlayerToMatch();
        else
            Debug.LogWarning("No GameplaySpawner found!");
    }
}