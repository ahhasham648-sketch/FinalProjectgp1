using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float startingHealth = 100f;
    [SerializeField, Tooltip("Health restored on respawn. 0 = full health.")] private float respawnHealth = 0f;

    [Header("Damage Response")]
    [SerializeField] private float invulnerabilityDuration = 1f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackUpForce = 3f;
    [SerializeField] private Color hurtFlashColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float hurtFlashInterval = 0.1f;

    [Header("Animation")]
    [SerializeField] private string hurtTrigger = "takehit";
    [SerializeField] private string deadBool = "dead";
    [SerializeField, Tooltip("Death state is played directly so it shows every time, whatever state he was in.")]
    private string deathStateName = "playerDeath";
    [SerializeField] private string idleStateName = "idle";

    [Header("Disable While Dead")]
    [SerializeField, Tooltip("Scripts turned off while dead (movement, attack...). Auto-filled if empty.")]
    private Behaviour[] disableOnDeath;

    [Header("Sounds")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.8f;

    [Header("Events")]
    public UnityEvent onDamaged;
    public UnityEvent onDied;
    public UnityEvent onRespawned;

    public event Action<float, float> HealthChanged;

    public float currentHealth { get; private set; }
    public float MaxHealth => startingHealth;
    public bool IsDead => dead;
    public bool IsInvulnerable => invulnerableTimer > 0f;

    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Color originalColor = Color.white;
    private bool dead;
    private float invulnerableTimer;
    private Coroutine flashRoutine;

    private void Awake()
    {
        if (startingHealth <= 0f) startingHealth = 100f;
        currentHealth = startingHealth;

        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        if (disableOnDeath == null || disableOnDeath.Length == 0)
        {
            disableOnDeath = new Behaviour[]
            {
                GetComponent<MoveWithPhysics>(),
                GetComponent<PlayerAttack>()
            };
        }
    }

    private void Start()
    {
        HealthChanged?.Invoke(currentHealth, startingHealth);
    }

    private void Update()
    {
        if (invulnerableTimer > 0f) invulnerableTimer -= Time.deltaTime;
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, null);
    }

    public void TakeDamage(float damage, Vector2? sourcePosition)
    {
        if (dead || damage <= 0f || IsInvulnerable) return;

        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, startingHealth);
        HealthChanged?.Invoke(currentHealth, startingHealth);

        if (currentHealth > 0f)
        {
            invulnerableTimer = invulnerabilityDuration;
            AnimatorUtil.SetTrigger(anim, hurtTrigger);
            Sfx.Play(hurtSound, soundVolume, 0.08f);

            if (sourcePosition.HasValue && rb != null)
            {
                float dir = Mathf.Sign(transform.position.x - sourcePosition.Value.x);
                if (dir == 0f) dir = 1f;
                rb.linearVelocity = new Vector2(dir * knockbackForce, knockbackUpForce);
                var move = GetComponent<MoveWithPhysics>();
                if (move != null) move.LockMovement(0.2f);
            }

            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(HurtFlash());
            onDamaged?.Invoke();
        }
        else
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (dead || amount <= 0f) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, startingHealth);
        HealthChanged?.Invoke(currentHealth, startingHealth);
    }

    public void Kill() => Kill(false);

    public void Kill(bool freezeInPlace)
    {
        if (dead) return;
        currentHealth = 0f;
        HealthChanged?.Invoke(currentHealth, startingHealth);
        Die();

        if (freezeInPlace && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void Die()
    {
        dead = true;

        AnimatorUtil.SetBool(anim, deadBool, true);
        AnimatorUtil.ResetTrigger(anim, hurtTrigger);
        if (anim != null && !string.IsNullOrEmpty(deathStateName) && anim.HasState(0, Animator.StringToHash(deathStateName)))
        {
            anim.Play(deathStateName, 0, 0f);
        }
        Sfx.Play(deathSound, soundVolume);

        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        SetControlsEnabled(false);

        var buffs = GetComponent<PlayerBuffs>();
        if (buffs != null) buffs.ClearAll();

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        onDied?.Invoke();
    }

    public void Respawn()
    {
        dead = false;
        currentHealth = respawnHealth > 0f ? Mathf.Min(respawnHealth, startingHealth) : startingHealth;
        invulnerableTimer = invulnerabilityDuration;

        if (anim != null)
        {
            AnimatorUtil.SetBool(anim, deadBool, false);
            AnimatorUtil.ResetTrigger(anim, hurtTrigger);
            if (!string.IsNullOrEmpty(idleStateName)) anim.Play(idleStateName);
        }

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }
        SetControlsEnabled(true);

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HurtFlash());

        HealthChanged?.Invoke(currentHealth, startingHealth);
        onRespawned?.Invoke();
    }

    private void SetControlsEnabled(bool enabled)
    {
        if (disableOnDeath == null) return;
        foreach (var b in disableOnDeath)
        {
            if (b != null) b.enabled = enabled;
        }
    }

    private IEnumerator HurtFlash()
    {
        if (spriteRenderer == null) yield break;

        while (invulnerableTimer > 0f && !dead)
        {
            spriteRenderer.color = spriteRenderer.color == originalColor ? hurtFlashColor : originalColor;
            yield return new WaitForSeconds(hurtFlashInterval);
        }

        spriteRenderer.color = originalColor;
        flashRoutine = null;
    }
}
