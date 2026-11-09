using UnityEngine;

public enum PotionType { Health, Attack, Speed }

[RequireComponent(typeof(Collider2D))]
public class PotionPickup : MonoBehaviour
{
    [Header("Potion")]
    [SerializeField] private PotionType type = PotionType.Health;

    [Header("Health Potion")]
    [SerializeField] private float healAmount = 40f;
    [SerializeField, Tooltip("If false, the potion stays on the ground while the player is at full health.")]
    private bool pickUpAtFullHealth = false;

    [Header("Attack Potion")]
    [SerializeField] private float attackMultiplier = 1.5f;
    [SerializeField] private float attackDuration = 10f;

    [Header("Speed Potion")]
    [SerializeField] private float speedMultiplier = 1.4f;
    [SerializeField] private float speedDuration = 8f;

    [Header("Look")]
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float bobSpeed = 2.5f;

    [Header("Sounds")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.8f;

    public PotionType Type => type;

    private Vector3 startPos;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var health = other.GetComponent<Health>();
        if (health == null || health.IsDead) return;

        switch (type)
        {
            case PotionType.Health:
                if (!pickUpAtFullHealth && health.currentHealth >= health.MaxHealth) return;
                health.Heal(healAmount);
                break;

            case PotionType.Attack:
            case PotionType.Speed:
                var buffs = other.GetComponent<PlayerBuffs>();
                if (buffs == null) buffs = other.gameObject.AddComponent<PlayerBuffs>();
                if (type == PotionType.Attack) buffs.ApplyAttackBoost(attackMultiplier, attackDuration);
                else buffs.ApplySpeedBoost(speedMultiplier, speedDuration);
                break;
        }

        Sfx.Play(pickupSound, soundVolume);
        Destroy(gameObject);
    }
}
