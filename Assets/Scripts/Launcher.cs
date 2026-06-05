using UnityEngine;
using TMPro; // Required for TMP components
using UnityEngine.UI; // Required for Button component
using Photon.Pun;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    [Header("UI Fields")]
    public TMP_InputField nameInputField;
    public TMP_InputField codeInputField;
    public TMP_Text statusText;

    [Header("Buttons")]
    public Button enterButton;
    public Button createButton;

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true; 
    
        PhotonNetwork.ConnectUsingSettings();
        statusText.text = "Connecting...";
        
        enterButton.interactable = false;
        createButton.interactable = false;

        codeInputField.characterLimit = 5;
        codeInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
    }

    public override void OnConnectedToMaster()
    {
        statusText.text = "Connected! Enter details to proceed.";
        enterButton.interactable = true;
        createButton.interactable = true;
        PhotonNetwork.JoinLobby();
    }

    // --- BUTTON ACTIONS ---

    public void OnClickCreateLobby()
    {
        if (ValidateInputs())
        {
            string roomName = codeInputField.text;
            PhotonNetwork.NickName = nameInputField.text;

            RoomOptions options = new RoomOptions { MaxPlayers = 10 };
            
            statusText.text = "Creating Lobby...";
            PhotonNetwork.CreateRoom(roomName, options);
        }
    }

    public void OnClickEnterLobby()
    {
        if (ValidateInputs())
        {
            PhotonNetwork.NickName = nameInputField.text;
            statusText.text = "Searching for Lobby...";
            PhotonNetwork.JoinRoom(codeInputField.text);
        }
    }

    // --- LOGIC & VALIDATION ---

    private bool ValidateInputs()
    {
        if (string.IsNullOrEmpty(nameInputField.text))
        {
            statusText.text = "Error: Please enter a name.";
            return false;
        }
        if (codeInputField.text.Length != 5)
        {
            statusText.text = "Error: Code must be exactly 5 digits.";
            return false;
        }
        return true;
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        statusText.text = "Error: This 5 digit code is already registered.";
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        statusText.text = "Error: Lobby not found. Check the code.";
    }


    public override void OnJoinedRoom()
{
    statusText.text = "Connected! Going to lobby...";
    
    Debug.Log($"Joined Room: {PhotonNetwork.CurrentRoom.Name} | IsMaster: {PhotonNetwork.IsMasterClient}");

    if (PhotonNetwork.IsMasterClient)
    {
        PhotonNetwork.LoadLevel("Lobby"); 
    }
}
}