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

        if (rb != null)
        rb.freezeRotation = true;

        healthBar.SetMaxHealth(maxPlayerHealth);
        healthBar.SetHealth(health);
    }

    void Update()
    {
        HandleJump();
        UpdateAnimations();
        CheckGrounded();
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine) return;
        HandleMovement();
    }
    // ── Movement ──────────────────────────────────────────────────────────────

    private void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(moveX, 0, moveZ).normalized;

        bool sprinting = Input.GetKey(sprintKey);
        float currentSpeed = sprinting ? sprintSpeed : speed;

        Vector3 targetVelocity = moveDirection * currentSpeed;

        // Smooth velocity change
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

        // Rotation
        if (moveDirection.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 12f * Time.deltaTime);
        }
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

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 inputDirection = new Vector3(moveX, 0, moveZ);

        float currentInputSpeed = inputDirection.magnitude * (Input.GetKey(sprintKey) ? sprintSpeed : speed);

        bool sprinting = Input.GetKey(sprintKey);
        bool moving    = currentInputSpeed > walkThreshold;
        bool jumping   = !isGrounded;

        // Use input-based speed instead of physics velocity
        animController.SetBool("isJumping", jumping);
        animController.SetBool("isRunning", !jumping && moving && sprinting);
        animController.SetBool("isWalking", !jumping && moving && !sprinting);
        animController.SetBool("isIdle",    !jumping && !moving);

        if (!string.IsNullOrEmpty(speedParamName))
        {
            animController.SetFloat(speedParamName, currentInputSpeed);
        }
    }

    // ── Ground detection ──────────────────────────────────────────────────────

    private void CheckGrounded()
    {
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f, ~LayerMask.GetMask("Player"));
    }

    // ── Health ────────────────────────────────────────────────────────────────

    void SetHealth(float healthChange)
    {
        health += healthChange;
        health  = Mathf.Clamp(health, 0, maxPlayerHealth);
        healthBar.SetHealth(health);
    }

    void DisablePlayer()
    {
        Destroy(this.gameObject);
    }
    // ── Damage / Collision ────────────────────────────────────────────────────

    private void OnCollisionEnter(Collision collision)
    {
        // Ground detection (keep this)
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                break;
            }
        }

        // === NEW: Damage from obstacles ===
        if (collision.gameObject.CompareTag("ParkourBlock"))   // ← Change tag if needed
        {
            float damage = 20f;     // Change this value as you like
            SetHealth(-damage);     // Negative = damage
        }
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