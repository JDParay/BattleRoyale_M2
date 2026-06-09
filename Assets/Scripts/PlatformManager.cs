using Photon.Pun;
using UnityEngine;
using System.Collections;

public class LobbyPlatform : MonoBehaviour
{
    public enum PlatformType { Ready, ChangeName, Leave }

    [Header("Platform Settings")]
    public PlatformType type;

    [Header("Leave Platform Only")]
    public float leaveHoldTime = 5f;

    [Header("Ready Platform Camera")]
    public Camera lobbyCamera;           // Assign in inspector
    public float readyCameraDelay = 0.5f; // Small delay before switching

    private float standingTime = 0f;
    private bool playerOnPlatform = false;
    private Nametag localNameTag;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main; // Cache the default main camera
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<PhotonView>().IsMine)
        {
            playerOnPlatform = true;
            standingTime = 0f;
            localNameTag = other.GetComponentInChildren<Nametag>();

            if (localNameTag != null)
            {
                if (type == PlatformType.ChangeName)
                {
                    Lobby3DManager.Instance.SetEditingStatus(true);
                    Lobby3DManager.Instance.ShowNameChangeUI();
                }
                
                if (type == PlatformType.Leave)
                {
                    localNameTag.StartLeaveCountdown(leaveHoldTime);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<PhotonView>().IsMine)
        {
            playerOnPlatform = false;
            standingTime = 0f;

            if (localNameTag != null)
            {
                if (type == PlatformType.ChangeName)
                    Lobby3DManager.Instance.SetEditingStatus(false);
                
                if (type == PlatformType.Leave)
                    localNameTag.StopLeaveCountdown();
            }

            if (type == PlatformType.Ready)
            {
                Lobby3DManager.Instance.SetReady(false);
                ResetToMainCamera(); // Reset if they step off before full ready
            }

            localNameTag = null;
        }
    }

    private void Update()
    {
        if (!playerOnPlatform) return;

        standingTime += Time.deltaTime;

        switch (type)
        {
            case PlatformType.Ready:
                if (standingTime > 0.4f)
                {
                    Lobby3DManager.Instance.ToggleLocalReady();
                    playerOnPlatform = false;

                    // Start camera transition
                    StartCoroutine(SwitchToLobbyCamera());
                }
                break;

            case PlatformType.Leave:
                if (standingTime >= leaveHoldTime)
                {
                    Lobby3DManager.Instance.StartLeaveProcess();
                    playerOnPlatform = false;
                }
                break;
        }
    }

    private IEnumerator SwitchToLobbyCamera()
    {
        yield return new WaitForSeconds(readyCameraDelay);

        if (mainCamera != null) mainCamera.enabled = false;
        if (lobbyCamera != null) lobbyCamera.enabled = true;

        // Optional: disable player movement/input while in lobby view
        // FindObjectOfType<PlayerController>()?.SetInputEnabled(false);
    }

    public void ResetToMainCamera()
    {
        if (mainCamera != null) mainCamera.enabled = true;
        if (lobbyCamera != null) lobbyCamera.enabled = false;
    }
}