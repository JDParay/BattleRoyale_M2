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

    private float standingTime = 0f;
    private bool playerOnPlatform = false;
    private Nametag localNameTag;

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

            // Fixed for new Lobby3DManager
            if (type == PlatformType.Ready)
            {
                Lobby3DManager.Instance.SetReady(false);
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
}