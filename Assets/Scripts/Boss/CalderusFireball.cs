using UnityEngine;

public class CalderusFireball : MonoBehaviour
{
    [Header("Fireball Settings")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private int damage = 20;
    [SerializeField] private float explosionDuration = 0.4f;
    [SerializeField] private float maximumLifetime = 6f;

    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D fireballCollider;

    private Vector2 moveDirection = Vector2.right;
    private bool hasExploded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        fireballCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        Destroy(gameObject, maximumLifetime);
    }

    private void FixedUpdate()
    {
        if (!hasExploded)
        {
            rb.linearVelocity = moveDirection * speed;
        }
    }

    public void Launch(Vector2 direction, Collider2D ownerCollider)
    {
        moveDirection = direction.normalized;

        if (ownerCollider != null)
        {
            Physics2D.IgnoreCollision(fireballCollider, ownerCollider);
        }

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x)
                      * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded)
        {
            return;
        }

        if (other.isTrigger && !other.CompareTag("Player"))
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            var health = other.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage, transform.position);
            }
        }

        Explode();
    }

    private void Explode()
    {
        hasExploded = true;

        rb.linearVelocity = Vector2.zero;
        fireballCollider.enabled = false;

        animator.SetTrigger("Explode");

        Destroy(gameObject, explosionDuration);
    }
}
