using System;
using UnityEngine;

public class PlayerBuffs : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField, Tooltip("Tint applied to the player while a boost is active.")]
    private Color attackTint = new Color(1f, 0.75f, 0.55f, 1f);
    [SerializeField] private Color speedTint = new Color(0.85f, 0.7f, 1f, 1f);
    [SerializeField] private bool tintWhileBuffed = true;

    [Header("Sounds")]
    [SerializeField] private AudioClip buffEndSound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.5f;

    public float DamageMultiplier => attackTimeLeft > 0f ? attackMultiplier : 1f;
    public float SpeedMultiplier => speedTimeLeft > 0f ? speedMultiplier : 1f;
    public float AttackTimeLeft => attackTimeLeft;
    public float SpeedTimeLeft => speedTimeLeft;

    public event Action Changed;

    private float attackMultiplier = 1f, speedMultiplier = 1f;
    private float attackTimeLeft, speedTimeLeft;
    private SpriteRenderer spriteRenderer;
    private Health health;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
    }

    public void ApplyAttackBoost(float multiplier, float duration)
    {
        attackMultiplier = Mathf.Max(1f, multiplier);
        attackTimeLeft = Mathf.Max(attackTimeLeft, duration);
        Changed?.Invoke();
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        speedMultiplier = Mathf.Max(1f, multiplier);
        speedTimeLeft = Mathf.Max(speedTimeLeft, duration);
        Changed?.Invoke();
    }

    public void ClearAll()
    {
        attackTimeLeft = speedTimeLeft = 0f;
        Changed?.Invoke();
    }

    private void Update()
    {
        bool hadAttack = attackTimeLeft > 0f, hadSpeed = speedTimeLeft > 0f;
        if (attackTimeLeft > 0f) attackTimeLeft -= Time.deltaTime;
        if (speedTimeLeft > 0f) speedTimeLeft -= Time.deltaTime;

        if ((hadAttack && attackTimeLeft <= 0f) || (hadSpeed && speedTimeLeft <= 0f))
        {
            Sfx.Play(buffEndSound, soundVolume);
            Changed?.Invoke();
        }

        if (tintWhileBuffed && spriteRenderer != null && (health == null || !health.IsInvulnerable))
        {
            Color target = Color.white;
            if (attackTimeLeft > 0f) target = attackTint;
            else if (speedTimeLeft > 0f) target = speedTint;
            spriteRenderer.color = target;
        }
    }
}
