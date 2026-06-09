using Photon.Pun;
using TMPro;
using UnityEngine;

public class Nametag : MonoBehaviour
{
    [Header("Text References")]
    public TMP_Text nameText;     
    public TMP_Text statusText;     
    public TMP_Text actionText;      
    public TMP_Text hostText;

    [Header("UI References")]
    public GameObject healthBar;     

    [Header("Positioning")]
    public Vector3 offset = new Vector3(0, 2.2f, 0);

    private PhotonView pv;
    private Camera cam;

    private float leaveTimer = 0f;
    private bool isLeaving = false;
    private bool isEditing = false;

    private void Awake()
    {
        pv = GetComponentInParent<PhotonView>();
        cam = Camera.main;
        transform.localPosition = offset;
    }

    private void Start()
{
    if (pv != null && pv.Owner != null && nameText != null)
    {
        nameText.text = pv.Owner.NickName;
    }

    UpdateHostIndicator();
}

// Separate this into a method so we can keep evaluating it if master client changes
public void UpdateHostIndicator()
{
    if (hostText == null || pv == null || pv.Owner == null) return;

    // Check if the owner of this specific network player clone is the Master Client
    if (pv.Owner.IsMasterClient)
    {
        hostText.text = "<color=yellow><b>HOST</b></color>";
    }
    else
    {
        hostText.text = "";
    }
}

    private void LateUpdate()
{
    if (cam == null) cam = Camera.main;
    if (cam != null) 
        transform.LookAt(transform.position + cam.transform.forward);

    if (pv != null && pv.Owner != null && nameText != null)
    {
        nameText.text = pv.Owner.NickName;
    }

    // === CHANGED: Use environment active state instead of scene name ===
    if (IsInLobbyPhase())
    {
        UpdateReadyStatus();
        UpdateHostIndicator();
        UpdateActionText();

        if (healthBar != null)
            {
                healthBar.SetActive(false);
            }
    }
    else
    {
        // Minigame phase
        if (statusText != null) statusText.text = "";
        if (actionText != null) actionText.text = "";
        
        // Keep winner text if it was set
        if (hostText != null && hostText.text != "<color=yellow><b>WINNER!</b></color>")
        {
            hostText.text = ""; 
        }

        if (healthBar != null)
            {
                healthBar.SetActive(true);
            }
    }
}

    public void RefreshName()
    {
        if (pv != null && pv.Owner != null && nameText != null)
        {
            nameText.text = pv.Owner.NickName;
        }
    }

    private void UpdateReadyStatus()
{
    if (statusText == null || pv?.Owner == null) return;

    bool isReady = false;
    if (pv.Owner.CustomProperties != null && 
        pv.Owner.CustomProperties.TryGetValue("IsReady", out object readyObj))
    {
        if (readyObj is bool b) isReady = b;
    }

    statusText.text = isReady ? 
        "<color=green>[READY]</color>" : 
        "<color=red>[NOT READY]</color>";
}

    private void UpdateActionText()
    {
        if (actionText == null || pv?.Owner == null) return;

        bool networkIsEditing = false;
        if (pv.Owner.CustomProperties.TryGetValue("IsEditing", out object editingObj))
        {
            networkIsEditing = (bool)editingObj;
        }

        if (isLeaving && leaveTimer > 0)
        {
            actionText.text = $"<color=orange>Leaving in {Mathf.Ceil(leaveTimer)}s</color>";
        }
        else if (networkIsEditing)
        {
            actionText.text = "[Currently Editing]";
        }
        else
        {
            actionText.text = "";
        }
    }

    private bool IsInLobbyPhase()
    {
        if (Lobby3DManager.Instance == null) return true;

        return Lobby3DManager.Instance.lobbyEnvironment != null && 
            Lobby3DManager.Instance.lobbyEnvironment.activeSelf;
    }

    // ====================== PUBLIC METHODS ======================

    public void StartLeaveCountdown(float duration)
    {
        isLeaving = true;
        leaveTimer = duration;
        StartCoroutine(LeaveCountdown());
    }

    public void StopLeaveCountdown()
    {
        isLeaving = false;
        leaveTimer = 0;
    }

    public void SetEditingMode(bool editing)
    {
        isEditing = editing;
    }

    public void ShowWinner()
{
    if (hostText != null)
        hostText.text = "<color=yellow><b>WINNER!</b></color>";
}

    private System.Collections.IEnumerator LeaveCountdown()
    {
        while (leaveTimer > 0 && isLeaving)
        {
            leaveTimer -= Time.deltaTime;
            yield return null;
        }
        isLeaving = false;
    }
}