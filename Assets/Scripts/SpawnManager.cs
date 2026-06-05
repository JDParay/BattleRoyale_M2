using Photon.Pun;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Player Settings")]
    public GameObject playerPrefab;

    [Header("Spawn Points - Exactly 3 recommended")]
    public Transform leftSpawn;
    public Transform middleSpawn; 
    public Transform rightSpawn;

    private void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady && playerPrefab != null)
        {
            SpawnPlayer();
        }
        else
        {
            Debug.LogError("❌ PlayerPrefab is missing or not connected!");
        }
    }

    private void SpawnPlayer()
    {
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (PhotonNetwork.IsMasterClient && middleSpawn != null)
        {
            spawnPos = middleSpawn.position;
            spawnRot = middleSpawn.rotation;
        }
        else if (leftSpawn != null && rightSpawn != null)
        {
            int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber;

            if (playerIndex % 2 == 0)
            {
                spawnPos = rightSpawn.position;
                spawnRot = rightSpawn.rotation;
                Debug.Log("→ Player spawned on RIGHT");
            }
            else
            {
                spawnPos = leftSpawn.position;
                spawnRot = leftSpawn.rotation;
                Debug.Log("← Player spawned on LEFT");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Spawn points not fully assigned. Spawning at origin.");
        }

        GameObject playerInstance = PhotonNetwork.Instantiate("Prefabs/PlayerPrefab", spawnPos, spawnRot);
        Debug.Log($"✅ Spawned player instance over network");

        if (playerInstance != null)
        {
            DontDestroyOnLoad(playerInstance);
        }
    }
}