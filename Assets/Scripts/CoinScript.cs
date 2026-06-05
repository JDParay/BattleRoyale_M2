using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class CoinScript : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    private Vector3 desiredPosition;
    private Quaternion desiredRotation;
    private bool isCollected = false;
    public AudioClip sfx;

    private void OnTriggerEnter(Collider other)
    {
        // Ensure only the local player hitting it triggers the collection logic
        if (!isCollected && other.CompareTag("Player") && other.GetComponent<PhotonView>().IsMine)
        {
            isCollected = true;
            
            // 1. Add score to custom properties
            int currentScore = 0;
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Score", out object scoreObj))
            {
                currentScore = (int)scoreObj;
            }
            
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable { { "Score", currentScore + 1 } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            AudioSource.PlayClipAtPoint(sfx, transform.position);

            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                photonView.RPC("RequestDestroyCoin", RpcTarget.MasterClient);
            }
        }
    }
  
    [PunRPC]
    private void RequestDestroyCoin()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        if (info.photonView.InstantiationData != null && info.photonView.InstantiationData.Length > 0)
        {
            Vector3 targetPos = (Vector3)info.photonView.InstantiationData[0];
            transform.position = targetPos;
            Debug.Log($"✅ Coin position forced to: {targetPos}");
        }
    }

    public void SetSpawnTransform(Vector3 pos, Quaternion rot)
    {
        desiredPosition = pos;
        desiredRotation = rot;
        
        transform.position = pos;
        transform.rotation = rot;
    }

    private void LateUpdate()
    {
        if (desiredPosition != Vector3.zero)
        {
            transform.position = desiredPosition;
            transform.rotation = desiredRotation;
        }
    }
}