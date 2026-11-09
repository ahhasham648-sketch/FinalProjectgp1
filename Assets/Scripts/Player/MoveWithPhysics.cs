using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MoveWithPhysics : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private Animator anim;
    private float horizontalInput;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    [SerializeField, Tooltip("Layers the player can stand on. Everything = any solid collider (floors, platforms...).")]
    private LayerMask groundLayers = ~0;
    [SerializeField] private float groundCheckDistance = 0.12f;
    [SerializeField, Tooltip("Only allow jumping while standing on the ground.")]
    private bool requireGroundToJump = true;
    [SerializeField, Tooltip("Grace period after walking off a ledge where a jump still works.")]
    private float coyoteTime = 0.1f;

    [Header("Animation Parameters")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string groundedParam = "Grounded";
    [SerializeField] private string yVelocityParam = "Yvelocity";
    [SerializeField] private string jumpTrigger = "Jump";

    public bool IsGrounded { get; private set; }

    private float lastGroundedTime = -10f;
    private float ignoreGroundUntil;
    private float movementLockedUntil;
    private readonly Collider2D[] groundHits = new Collider2D[8];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        buffs = GetComponent<PlayerBuffs>();
    }

    private PlayerBuffs buffs;
    private float CurrentSpeed
    {
        get
        {
            if (buffs == null) buffs = GetComponent<PlayerBuffs>();
            return moveSpeed * (buffs != null ? buffs.SpeedMultiplier : 1f);
        }
    }

    private void OnDisable()
    {
        horizontalInput = 0f;
        AnimatorUtil.SetFloat(anim, speedParam, 0f);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");

        UpdateGrounded();

        bool canJump = !requireGroundToJump || Time.time - lastGroundedTime <= coyoteTime;
        if (Input.GetKeyDown(jumpKey) && canJump)
        {
            Jump();
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (Time.time < movementLockedUntil) return;

        rb.linearVelocity = new Vector2(
            horizontalInput * CurrentSpeed,
            rb.linearVelocity.y
        );
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            jumpForce
        );

        ignoreGroundUntil = Time.time + 0.1f;
        lastGroundedTime = -10f;
        IsGrounded = false;
        AnimatorUtil.SetTrigger(anim, jumpTrigger);
    }

    public void LockMovement(float seconds)
    {
        movementLockedUntil = Mathf.Max(movementLockedUntil, Time.time + seconds);
    }

    private void UpdateGrounded()
    {
        if (Time.time < ignoreGroundUntil || bodyCollider == null)
        {
            IsGrounded = false;
            return;
        }

        Bounds b = bodyCollider.bounds;
        Vector2 center = new Vector2(b.center.x, b.min.y - groundCheckDistance * 0.5f);
        Vector2 size = new Vector2(b.size.x * 0.9f, groundCheckDistance);

        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(groundLayers.value != 0 ? groundLayers : (LayerMask)Physics2D.AllLayers);
        int count = Physics2D.OverlapBox(center, size, 0f, filter, groundHits);

        bool grounded = false;
        for (int i = 0; i < count; i++)
        {
            var hit = groundHits[i];
            if (hit == null || hit.isTrigger || hit.attachedRigidbody == rb) continue;
            grounded = true;
            break;
        }

        if (rb.linearVelocity.y > 0.5f) grounded = false;

        IsGrounded = grounded;
        if (grounded) lastGroundedTime = Time.time;
    }

    private void UpdateAnimator()
    {
        if (anim == null) return;
        AnimatorUtil.SetFloat(anim, speedParam, Mathf.Abs(horizontalInput * CurrentSpeed));
        AnimatorUtil.SetBool(anim, groundedParam, IsGrounded);
        AnimatorUtil.SetFloat(anim, yVelocityParam, rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;
        Bounds b = col.bounds;
        Gizmos.color = IsGrounded ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(new Vector3(b.center.x, b.min.y - groundCheckDistance * 0.5f), new Vector3(b.size.x * 0.9f, groundCheckDistance));
    }
}
