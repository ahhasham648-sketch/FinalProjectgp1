using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    [Header("Info")]
    [SerializeField] private string displayName = "";

    [Header("Health")]
    [SerializeField] private float maxHealth = 75f;
    [SerializeField] private float invulnerabilityAfterHit = 0.2f;

    [Header("Damage Response")]
    [SerializeField] private float knockbackDistance = 0.3f;
    [SerializeField, Tooltip("Walking enemies: knockback never pushes them off a ledge or into a wall. Turn off for flyers.")]
    private bool keepOnGround = true;
    [SerializeField, Tooltip("Lava and other hazards can't hurt this enemy (bosses).")]
    private bool immuneToHazards = false;
    [SerializeField, Tooltip("If the enemy somehow falls below this height it dies (so the level can't get stuck).")]
    private float fallDeathY = -15f;
    [SerializeField] private Color hurtFlashColor = new Color(1f, 0.4f, 0.4f, 1f);
    [SerializeField] private float hurtFlashDuration = 0.12f;

    [Header("Death")]
    [SerializeField, Tooltip("Seconds after death before the body disappears. Negative = never.")]
    private float hideBodyDelay = 2f;
    [SerializeField, Tooltip("Scripts turned off on death (patrol, attack...). Auto-filled if empty.")]
    private Behaviour[] disableOnDeath;

    [Header("Animation")]
    [SerializeField] private string hurtTrigger = "takehit";
    [SerializeField] private string deadBool = "dead";
    [SerializeField] private string speedFloat = "speed";

    [Header("Sounds")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.8f;

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onDeath;

    public event Action<EnemyHealth> Died;
    public event Action<float, float> HealthChanged;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? gameObject.name : displayName;
    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }
    public bool ImmuneToHazards => immuneToHazards;

    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Color originalColor = Color.white;
    private float invulnerableTimer;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        if (disableOnDeath == null || disableOnDeath.Length == 0)
        {
            var list = new System.Collections.Generic.List<Behaviour>();
            foreach (var mb in GetComponents<MonoBehaviour>())
            {
                if (mb != this && (mb is EnemyAttack || mb.GetType().Name.Contains("Patrol"))) list.Add(mb);
            }
            disableOnDeath = list.ToArray();
        }
    }

    private void Update()
    {
        if (invulnerableTimer > 0f) invulnerableTimer -= Time.deltaTime;

        if (!IsDead && transform.position.y < fallDeathY)
        {
            CurrentHealth = 0f;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            Die();
        }
    }

    private bool CanMoveTo(float dx)
    {
        if (!keepOnGround) return true;

        var body = GetComponent<Collider2D>();
        if (body == null) return true;
        Bounds b = body.bounds;

        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(Physics2D.AllLayers);
        var hits = new RaycastHit2D[8];

        Vector2 footOrigin = new Vector2(b.center.x + dx + Mathf.Sign(dx) * b.extents.x * 0.5f, b.min.y + 0.2f);
        bool ground = false;
        int n = Physics2D.Raycast(footOrigin, Vector2.down, filter, hits, 1.2f);
        for (int i = 0; i < n; i++) if (!IsSelf(hits[i].collider)) { ground = true; break; }
        if (!ground) return false;

        Vector2 bodyOrigin = new Vector2(b.center.x, b.center.y);
        n = Physics2D.Raycast(bodyOrigin, new Vector2(Mathf.Sign(dx), 0f), filter, hits, b.extents.x + Mathf.Abs(dx));
        for (int i = 0; i < n; i++)
        {
            var c = hits[i].collider;
            if (IsSelf(c) || c.CompareTag("Player")) continue;
            return false;
        }
        return true;
    }

    private bool IsSelf(Collider2D c) => c != null && (c.transform == transform || c.transform.IsChildOf(transform));

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, null);
    }

    public void TakeDamage(float damage, Vector2? sourcePosition)
    {
        if (IsDead || damage <= 0f || invulnerableTimer > 0f) return;

        CurrentHealth = Mathf.Clamp(CurrentHealth - damage, 0f, maxHealth);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth > 0f)
        {
            invulnerableTimer = invulnerabilityAfterHit;
            AnimatorUtil.SetTrigger(anim, hurtTrigger);
            Sfx.Play(hurtSound, soundVolume, 0.1f);

            if (sourcePosition.HasValue && knockbackDistance > 0f)
            {
                float dir = Mathf.Sign(transform.position.x - sourcePosition.Value.x);
                if (CanMoveTo(dir * knockbackDistance))
                    transform.position += new Vector3(dir * knockbackDistance, 0f, 0f);
            }

            if (spriteRenderer != null) StartCoroutine(Flash());
            onDamaged?.Invoke();
        }
        else
        {
            Die();
        }
    }

    private void Die()
    {
        IsDead = true;

        foreach (var b in disableOnDeath)
        {
            if (b != null) b.enabled = false;
        }

        AnimatorUtil.SetFloat(anim, speedFloat, 0f);
        AnimatorUtil.SetBool(anim, deadBool, true);
        Sfx.Play(deathSound, soundVolume);

        foreach (var col in GetComponents<Collider2D>()) col.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        onDeath?.Invoke();
        Died?.Invoke(this);

        if (hideBodyDelay >= 0f) StartCoroutine(HideAfterDelay());
    }

    private IEnumerator Flash()
    {
        spriteRenderer.color = hurtFlashColor;
        yield return new WaitForSeconds(hurtFlashDuration);
        if (!IsDead) spriteRenderer.color = originalColor;
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideBodyDelay);
        gameObject.SetActive(false);
    }
}
