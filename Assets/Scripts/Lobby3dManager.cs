using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
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

    private PhotonView pv;

    private void Awake()
    {
        Instance = this;
        pv = GetComponent<PhotonView>();
        if (pv == null)
        {
            pv = gameObject.AddComponent<PhotonView>();
            Debug.LogWarning("✅ Added missing PhotonView to Lobby3DManager");
        }

        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        SetReady(false);
    }

    // ====================== NAME CHANGE ======================
    public void ShowNameChangeUI()
    {
        if (nameChangePanel == null || nameInputField == null) return;

        nameInputField.text = PhotonNetwork.NickName;
        nameChangePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        nameInputField.ActivateInputField();
    }

    public void HideNameChangeUI()
    {
        if (nameChangePanel != null) nameChangePanel.SetActive(false);
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