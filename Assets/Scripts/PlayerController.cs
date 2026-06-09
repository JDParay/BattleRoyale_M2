using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun, IPunObservable
{
    [Header("Player Health Settings")]
    public float health = 100;
    public float maxPlayerHealth = 100;
    [SerializeField] private PlayerHealthBarUI healthBar;

    public float speed = 5f;
    public float jumpForce = 5f;
    private Rigidbody rb;
    private PhotonView pv;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        healthBar.SetMaxHealth(maxPlayerHealth);
        healthBar.SetHealth(health);
    }

    void Update()
    {
        if (!photonView.IsMine) return; 

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(moveX, 0, moveZ) * speed;
        rb.MovePosition(transform.position + move * Time.deltaTime);

        if (Input.GetButtonDown("Jump"))
        {
            Jump();
        }

        // TESTING HEALTH BAR
        if (Input.GetKeyDown(KeyCode.O))
        {
            SetHealth(-20);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            SetHealth(20);
        }

        if (health == 0)
        {
            KillPlayer();
        }
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    public void SetHealth(float healthChange)
    {
        health += healthChange;
        health = Mathf.Clamp(health, 0, maxPlayerHealth);

        healthBar.SetHealth(health);
    }

    void KillPlayer()
    {
        Destroy(this.gameObject);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(health);
        }
        else
        {
            health = (float)stream.ReceiveNext();
            healthBar.SetHealth(health);
        }
    }
}