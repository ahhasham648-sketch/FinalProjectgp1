using UnityEngine;

public class CalderusBoss : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform firePoint;
    [SerializeField] private CalderusFireball fireballPrefab;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Arena")]
    [SerializeField, Tooltip("Keep Calderus inside his arena so he can't be lured off the edge into the lava.")]
    private bool stayInArena = true;
    [SerializeField] private float arenaMinX = 64f;
    [SerializeField] private float arenaMaxX = 98.5f;
    [SerializeField, Tooltip("If he ever ends up this far below where he started, he is put back at his start position.")]
    private float fallResetDepth = 3f;

    [Header("Attacks")]
    [SerializeField] private float meleeRange = 2f;
    [SerializeField] private float rangedAttackRange = 8f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int meleeDamage = 30;
    [SerializeField, Tooltip("Safety: if an attack animation event never fires, the attack ends after this long.")]
    private float attackTimeout = 1.2f;

    [Header("Hurt")]
    [SerializeField, Tooltip("How long he staggers when hit.")]
    private float hurtDuration = 0.3f;
    [SerializeField, Tooltip("Minimum time between staggers, so he can't be stun-locked.")]
    private float flinchCooldown = 2.5f;

    [Header("Enrage")]
    [SerializeField, Range(0f, 1f)] private float enrageAtHealthFraction = 0.5f;
    [SerializeField] private float enragedSpeedMultiplier = 1.35f;
    [SerializeField] private float enragedCooldownMultiplier = 0.65f;
    [SerializeField] private Color enragedTint = new Color(1f, 0.6f, 0.5f, 1f);

    [Header("Health (used only if there is no EnemyHealth component)")]
    [SerializeField] private int maximumHealth = 300;

    [Header("Boss Bar")]
    [SerializeField] private string bossTitle = "CALDERUS";

    [Header("Sounds")]
    [SerializeField] private AudioClip roarSound;
    [SerializeField] private AudioClip meleeSound;
    [SerializeField] private AudioClip fireballSound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.85f;

    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D bossCollider;
    private EnemyHealth health;
    private Health playerHealth;
    private SpriteRenderer spriteRenderer;

    private int currentHealth;
    private float nextAttackTime;
    private float attackEndsAt;
    private float hurtEndsAt;
    private float nextFlinchTime;
    private bool isAttacking;
    private bool isHurt;
    private bool isDead;
    private bool engaged;
    private bool enraged;
    private Vector3 spawnPosition;

    private float SpeedNow => moveSpeed * (enraged ? enragedSpeedMultiplier : 1f);
    private float CooldownNow => attackCooldown * (enraged ? enragedCooldownMultiplier : 1f);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        bossCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<EnemyHealth>();

        currentHealth = maximumHealth;
        spawnPosition = transform.position;

        if (health != null)
        {
            health.onDamaged.AddListener(OnDamaged);
            health.Died += _ => Die();
        }
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }
        if (player != null) playerHealth = player.GetComponent<Health>();
    }

    private void Update()
    {
        if (isDead || player == null)
        {
            StopMoving();
            return;
        }

        KeepInArena();

        if (isAttacking && Time.time >= attackEndsAt) isAttacking = false;
        if (isHurt && Time.time >= hurtEndsAt) isHurt = false;

        if (playerHealth != null && playerHealth.IsDead)
        {
            StopMoving();
            return;
        }

        if (isAttacking || isHurt)
        {
            StopMoving();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer > detectionRange && !engaged)
        {
            StopMoving();
            return;
        }

        if (!engaged)
        {
            engaged = true;
            Sfx.Play(roarSound, soundVolume);
            if (health != null && GameUIController.Instance != null)
                GameUIController.Instance.ShowBossBar(health, bossTitle);
        }

        FacePlayer();

        if (Time.time >= nextAttackTime)
        {
            if (distanceToPlayer <= meleeRange)
            {
                BeginAttack("Attack1");
                return;
            }

            if (distanceToPlayer <= rangedAttackRange)
            {
                BeginAttack("Attack2");
                return;
            }
        }

        if (distanceToPlayer > meleeRange) ChasePlayer();
        else StopMoving();
    }

    private void ChasePlayer()
    {
        float direction = Mathf.Sign(player.position.x - transform.position.x);

        if (stayInArena &&
            ((direction < 0f && transform.position.x <= arenaMinX) ||
             (direction > 0f && transform.position.x >= arenaMaxX)))
        {
            StopMoving();
            return;
        }

        rb.linearVelocity = new Vector2(direction * SpeedNow, rb.linearVelocity.y);
        animator.SetBool("IsRunning", true);
    }

    private void KeepInArena()
    {
        if (rb == null) return;

        if (transform.position.y < spawnPosition.y - fallResetDepth)
        {
            rb.position = spawnPosition;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (!stayInArena) return;

        float clampedX = Mathf.Clamp(rb.position.x, arenaMinX, arenaMaxX);
        if (!Mathf.Approximately(clampedX, rb.position.x))
        {
            rb.position = new Vector2(clampedX, rb.position.y);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private void StopMoving()
    {
        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (animator != null) animator.SetBool("IsRunning", false);
    }

    private void FacePlayer()
    {
        bool playerIsRight = player.position.x > transform.position.x;
        float correctDirection = playerIsRight == spriteFacesRight ? 1f : -1f;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * correctDirection;
        transform.localScale = scale;
    }

    private void BeginAttack(string triggerName)
    {
        isAttacking = true;
        nextAttackTime = Time.time + CooldownNow;
        attackEndsAt = Time.time + attackTimeout;

        StopMoving();
        animator.SetTrigger(triggerName);
    }

    public void FinishAttack()
    {
        isAttacking = false;
    }

    public void DealMeleeDamage()
    {
        if (player == null || isDead) return;

        Sfx.Play(meleeSound, soundVolume, 0.08f);
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= meleeRange + 0.5f && playerHealth != null)
        {
            playerHealth.TakeDamage(meleeDamage, transform.position);
        }
    }

    public void LaunchFireball()
    {
        if (fireballPrefab == null || firePoint == null || player == null || isDead) return;

        CalderusFireball newFireball = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
        Vector2 direction = (player.position - firePoint.position).normalized;
        newFireball.Launch(direction, bossCollider);
        Sfx.Play(fireballSound, soundVolume, 0.1f);
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0) return;

        if (health != null)
        {
            health.TakeDamage(damage);
            return;
        }

        currentHealth -= damage;
        if (currentHealth <= 0) { Die(); return; }
        OnDamaged();
    }

    private void OnDamaged()
    {
        if (isDead) return;

        if (!enraged && health != null && health.CurrentHealth <= health.MaxHealth * enrageAtHealthFraction)
        {
            enraged = true;
            if (spriteRenderer != null) spriteRenderer.color = enragedTint;
            Sfx.Play(roarSound, soundVolume);
        }

        if (Time.time < nextFlinchTime) return;

        nextFlinchTime = Time.time + flinchCooldown;
        isAttacking = false;
        isHurt = true;
        hurtEndsAt = Time.time + hurtDuration;

        StopMoving();
        animator.SetTrigger("Hurt");
    }

    public void FinishHurt()
    {
        isHurt = false;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        isAttacking = false;
        isHurt = false;

        StopMoving();

        animator.ResetTrigger("Attack1");
        animator.ResetTrigger("Attack2");
        animator.ResetTrigger("Hurt");
        animator.SetTrigger("Die");

        if (bossCollider != null) bossCollider.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (GameUIController.Instance != null) GameUIController.Instance.HideBossBar();
    }
}
