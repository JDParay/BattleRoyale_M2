using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPun, IPunObservable
{
    [Header("Player Health Settings")]
    public float health = 100;
    public float maxPlayerHealth = 100;
    // public bool playerDisabled = false;
    // public float playerDisabledTimer = 0;
    public float maxPlayerDisableTime = 10f;
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
        // Debug.Log($"Player disabled: {playerDisabled}. Disabled timer: " + playerDisabledTimer);

        if (!photonView.IsMine) return;

        if (health == 0)
        {
            // playerDisabled = true;
            // KillPlayer();
        }

        // run timer
        // if (playerDisabled)
        // {
        //     playerDisabledTimer += Time.deltaTime;
        //     rb.Sleep();
        //     if (playerDisabledTimer >= maxPlayerDisableTime)
        //     {
        //         // resets player
        //         playerDisabledTimer = 0f;
        //         SetHealth(maxPlayerHealth);
        //         playerDisabled = false;
        //         rb.WakeUp();
        //     }
        // }

        // if (playerDisabled) return;

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

    void DisablePlayer()
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