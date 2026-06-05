using UnityEngine;
using Photon.Pun;

public class CoinSpawner : MonoBehaviour
{
    public Transform[] spawnPoints;
    public float spawnInterval = 2.5f;

    private float timer = 0f;
    private bool canSpawn = false;

    public void StartSpawning() => canSpawn = true;
    public void StopSpawning() => canSpawn = false;

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient || !canSpawn || spawnPoints.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnCoin();
        }
    }

    private void SpawnCoin()
    {
        if (!PhotonNetwork.IsMasterClient || spawnPoints == null || spawnPoints.Length == 0) 
            return;

        int index = Random.Range(0, spawnPoints.Length);
        Transform targetPoint = spawnPoints[index];

        Vector3 spawnPos = targetPoint.position;
        Quaternion spawnRot = targetPoint.rotation;   // ← Take rotation too

        Debug.Log($"Spawning coin at {spawnPos} | Rotation: {spawnRot.eulerAngles}");

        GameObject coin = PhotonNetwork.InstantiateRoomObject("Prefabs/Coin", spawnPos, spawnRot);

        if (coin != null)
        {
            coin.GetComponent<CoinScript>().SetSpawnTransform(spawnPos, spawnRot);
        }
    }
}