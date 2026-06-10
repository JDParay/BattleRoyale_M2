using Photon.Pun;
using UnityEngine;

public class ChangeColor : MonoBehaviourPun
{
    private Renderer targetRenderer;
    private static readonly string PrefKey = "PlayerColorIndex";

    private void Awake()
    {
        SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>();

        if (smr != null)
        {
            targetRenderer = smr;
            // Create unique material instance so it doesn't affect other players
            targetRenderer.material = new Material(targetRenderer.sharedMaterial);
            Debug.Log($"✅ Renderer found: {targetRenderer.name}");
        }
        else
        {
            Debug.LogError("❌ No SkinnedMeshRenderer found in children!");
        }
    }

    private void Start()
    {
        if (!photonView.IsMine) return;

        if (PlayerPrefs.HasKey("PlayerColorR"))
        {
            Color saved = new Color(
                PlayerPrefs.GetFloat("PlayerColorR"),
                PlayerPrefs.GetFloat("PlayerColorG"),
                PlayerPrefs.GetFloat("PlayerColorB")
            );
            ApplyColor(saved);
        }
    }

    public void ApplyColor(Color chosen)
    {
        if (targetRenderer == null)
        {
            Debug.LogError("❌ targetRenderer is null!");
            return;
        }

        // Apply color
        targetRenderer.material.SetColor("_BaseColor", chosen);
        Debug.Log($"✅ Color applied: {chosen}");

        // Save locally
        PlayerPrefs.SetFloat("PlayerColorR", chosen.r);
        PlayerPrefs.SetFloat("PlayerColorG", chosen.g);
        PlayerPrefs.SetFloat("PlayerColorB", chosen.b);

        // Sync to other players
        photonView.RPC(nameof(RPC_SyncColor), RpcTarget.OthersBuffered,
                       chosen.r, chosen.g, chosen.b);

        // Store in Photon for late-joiners
        var props = new ExitGames.Client.Photon.Hashtable();
        props["color"] = new float[] { chosen.r, chosen.g, chosen.b };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    [PunRPC]
    private void RPC_SyncColor(float r, float g, float b)
    {
        if (targetRenderer == null) return;
        targetRenderer.material.SetColor("_BaseColor", new Color(r, g, b));
    }

    public void ApplyFromPhotonProperties(ExitGames.Client.Photon.Hashtable props)
    {
        if (props.TryGetValue("color", out object val) && val is float[] rgb)
        {
            if (targetRenderer == null) return;
            targetRenderer.material.SetColor("_BaseColor", new Color(rgb[0], rgb[1], rgb[2]));
        }
    }
}