using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun, IPunObservable
{
    [Header("Player Health Settings")]
    public float health = 100;
    public float maxPlayerHealth = 100;
    [SerializeField] private PlayerHealthBarUI healthBar;

    [Header("Movement Settings")]
    public float speed = 5f;
    public float sprintSpeed = 9f;
    public float jumpForce = 5f;

    [Header("Sprint Key")]
    [Tooltip("Hold this key to sprint. Default is Left Shift.")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Animation Settings")]
    [Tooltip("Minimum XZ velocity magnitude to transition from Idle to Walk.")]
    [SerializeField] private float walkThreshold = 0.1f;
    [Tooltip("Animator speed parameter driven by current move speed (optional — leave blank to skip).")]
    [SerializeField] private string speedParamName = "MoveSpeed";

    private Rigidbody rb;
    private Animator  animController;
    private PhotonView pv;

    private bool isGrounded = true;

    void Awake()
    {
        pv             = GetComponent<PhotonView>();
        animController = GetComponent<Animator>();
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

        HandleMovement();
        HandleJump();
        UpdateAnimations();
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    private void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        bool  sprinting   = Input.GetKey(sprintKey);
        float currentSpeed = sprinting ? sprintSpeed : speed;

        Vector3 move = new Vector3(moveX, 0, moveZ) * currentSpeed;
        rb.MovePosition(transform.position + move * Time.deltaTime);
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private void UpdateAnimations()
    {
        if (animController == null) return;

        // Derive horizontal speed from the rigidbody so it's physics-accurate
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float   currentSpeed       = horizontalVelocity.magnitude;

        bool sprinting = Input.GetKey(sprintKey);
        bool moving    = currentSpeed > walkThreshold;
        bool jumping   = !isGrounded;

        // Mutually exclusive states — order: jump > run > walk > idle
        animController.SetBool("isJumping",  jumping);
        animController.SetBool("isRunning",  !jumping && moving && sprinting);
        animController.SetBool("isWalking",  !jumping && moving && !sprinting);
        animController.SetBool("isIdle",     !jumping && !moving);

        // Optional float for blend-tree speed (ignored if param doesn't exist)
        if (!string.IsNullOrEmpty(speedParamName))
        {
            animController.SetFloat(speedParamName, currentSpeed);
        }
    }

    // ── Ground detection ──────────────────────────────────────────────────────

    void OnCollisionEnter(Collision collision)
    {
        // Treat any collision below the player as landing
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                break;
            }
        }
    }

    // ── Health ────────────────────────────────────────────────────────────────

    void SetHealth(float healthChange)
    {
        health += healthChange;
        health  = Mathf.Clamp(health, 0, maxPlayerHealth);
        healthBar.SetHealth(health);
    }

    // ── PUN sync ──────────────────────────────────────────────────────────────

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