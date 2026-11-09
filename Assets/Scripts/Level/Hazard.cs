using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Hazard : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField, Tooltip("Lava: instant death (costs a life, respawn at checkpoint).")]
    private bool instantKill = true;
    [SerializeField] private float damage = 35f;
    [SerializeField, Tooltip("Seconds between hits while standing in it (when not instant kill).")]
    private float damageInterval = 0.75f;
    [SerializeField] private bool killEnemiesToo = true;

    [Header("Look")]
    [SerializeField] private bool pulseGlow = true;
    [SerializeField] private Color glowColorA = new Color(1f, 0.85f, 0.6f, 1f);
    [SerializeField] private Color glowColorB = new Color(1f, 0.55f, 0.35f, 1f);
    [SerializeField] private float pulseSpeed = 1.6f;

    [Header("Sounds")]
    [SerializeField] private AudioClip burnSound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.8f;

    private SpriteRenderer spriteRenderer;
    private float nextHitTime;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (pulseGlow && spriteRenderer != null)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed + transform.position.x * 0.3f) + 1f) * 0.5f;
            spriteRenderer.color = Color.Lerp(glowColorB, glowColorA, t);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) => Touch(other);
    private void OnTriggerStay2D(Collider2D other) => Touch(other);

    private void Touch(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var health = other.GetComponent<Health>();
            if (health == null || health.IsDead) return;

            if (instantKill)
            {
                Sfx.Play(burnSound, soundVolume);
                health.Kill(true);
            }
            else if (Time.time >= nextHitTime)
            {
                nextHitTime = Time.time + damageInterval;
                Sfx.Play(burnSound, soundVolume, 0.1f);
                health.TakeDamage(damage, new Vector2(other.transform.position.x, transform.position.y - 1f));
            }
            return;
        }

        if (killEnemiesToo)
        {
            var enemy = other.GetComponentInParent<EnemyHealth>();
            if (enemy != null && !enemy.IsDead && !enemy.ImmuneToHazards) enemy.TakeDamage(99999f);
        }
    }
}
