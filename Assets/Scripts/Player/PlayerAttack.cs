using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode attackKey = KeyCode.J;
    [SerializeField] private bool attackWithLeftMouse = true;

    [Header("Damage")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float attackCooldown = 0.45f;
    [SerializeField, Tooltip("Delay between pressing attack and the hit landing (sync with the swing animation).")]
    private float hitDelay = 0.15f;

    [Header("Hit Area")]
    [SerializeField, Tooltip("Optional. If empty, attackOffset from the player is used.")]
    private Transform attackPoint;
    [SerializeField] private Vector2 attackOffset = new Vector2(0.9f, 0.3f);
    [SerializeField, Tooltip("Width x height of the hit box. Tall enough to reach flying enemies.")]
    private Vector2 attackSize = new Vector2(1.8f, 2.8f);
    [SerializeField] private LayerMask enemyLayers = ~0;

    [Header("Facing")]
    [SerializeField] private bool flipSpriteWithMovement = true;
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Animation")]
    [SerializeField] private string[] attackTriggers = { "Attack1", "Attack2" };

    [Header("Sounds")]
    [SerializeField] private AudioClip swingSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.8f;

    public int FacingDirection { get; private set; } = 1;

    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private float cooldownTimer;
    private int comboIndex;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;

        UpdateFacing();

        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        bool pressed = Input.GetKeyDown(attackKey) || (attackWithLeftMouse && Input.GetMouseButtonDown(0));
        if (pressed && cooldownTimer <= 0f)
        {
            StartCoroutine(Attack());
        }
    }

    private void UpdateFacing()
    {
        float h = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(h) > 0.01f) FacingDirection = h > 0f ? 1 : -1;

        if (flipSpriteWithMovement && spriteRenderer != null)
        {
            spriteRenderer.flipX = spriteFacesRight ? FacingDirection < 0 : FacingDirection > 0;
        }
    }

    private IEnumerator Attack()
    {
        cooldownTimer = attackCooldown;

        if (attackTriggers != null && attackTriggers.Length > 0)
        {
            AnimatorUtil.SetTrigger(anim, attackTriggers[comboIndex % attackTriggers.Length]);
            comboIndex++;
        }
        Sfx.Play(swingSound, soundVolume, 0.1f);

        if (hitDelay > 0f) yield return new WaitForSeconds(hitDelay);

        var hitEnemies = new HashSet<EnemyHealth>();
        foreach (var col in Physics2D.OverlapBoxAll(GetAttackPosition(), attackSize, 0f, enemyLayers))
        {
            var enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy != null && !enemy.IsDead && hitEnemies.Add(enemy))
            {
                var buffs = GetComponent<PlayerBuffs>();
                enemy.TakeDamage(damage * (buffs != null ? buffs.DamageMultiplier : 1f), transform.position);
            }
        }

        if (hitEnemies.Count > 0) Sfx.Play(hitSound, soundVolume, 0.1f);
    }

    private Vector2 GetAttackPosition()
    {
        if (attackPoint != null) return attackPoint.position;
        return (Vector2)transform.position + new Vector2(attackOffset.x * FacingDirection, attackOffset.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 pos = attackPoint != null
            ? (Vector2)attackPoint.position
            : (Vector2)transform.position + new Vector2(attackOffset.x * (Application.isPlaying ? FacingDirection : 1), attackOffset.y);
        Gizmos.DrawWireCube(pos, attackSize);
    }
}
