using UnityEngine;
using Photon.Pun;

public class GameplaySpawner : MonoBehaviour
{
    [Header("Gameroom Spawn Points")]
    public Transform[] gameSpawnPoints;

    public void MoveExistingPlayerToMatch()
    {
        if (gameSpawnPoints == null || gameSpawnPoints.Length == 0) return;

        GameObject localPlayerObj = null;
        PhotonView[] allViews = FindObjectsByType<PhotonView>(FindObjectsSortMode.None);

        // Find our active local player avatar
        foreach (var view in allViews)
        {
            if (view.IsMine && view.CompareTag("Player"))
            {
                localPlayerObj = view.gameObject;
                break;
            }
        }

        if (localPlayerObj != null)
        {
            int myIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
            int spawnIndex = myIndex % gameSpawnPoints.Length; 

            Transform targetSpawn = gameSpawnPoints[spawnIndex];

            // Turn off character physics tracking so the teleport is instant and flawless
            var controller = localPlayerObj.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            
            // Move into the minigame map coordinates
            localPlayerObj.transform.position = targetSpawn.position;
            localPlayerObj.transform.rotation = targetSpawn.rotation;

            if (controller != null) controller.enabled = true;
            
            Debug.Log($"✅ Repositioned player to match starting grid: {spawnIndex}");
        }
    }
}