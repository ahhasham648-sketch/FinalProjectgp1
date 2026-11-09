using System.Collections;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Target")]
    [SerializeField, Tooltip("Optional. Found by the 'Player' tag if empty.")]
    private Health target;

    [Header("Damage")]
    [SerializeField] private float damage = 15f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField, Tooltip("Wind-up time before the hit lands (sync with the attack animation).")]
    private float hitDelay = 0.45f;

    [Header("Range")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float verticalTolerance = 1.5f;
    [SerializeField, Tooltip("Within this range the enemy stops patrolling and turns to face the player.")]
    private float engageRange = 3f;

    [Header("Behaviour")]
    [SerializeField] private bool stopPatrolWhileEngaged = true;
    [SerializeField] private bool spriteFacesRight = true;
    [SerializeField, Tooltip("Patrol scripts to pause while fighting. Auto-filled if empty.")]
    private Behaviour[] patrolScripts;

    [Header("Flying Dive (Bat, Flying Eye)")]
    [SerializeField, Tooltip("Swoop down to the target's height to strike, then fly back up.")]
    private bool diveAtTarget = false;
    [SerializeField] private float diveSpeed = 7f;
    [SerializeField] private float returnSpeed = 3f;
    [SerializeField, Tooltip("Where the enemy's centre aims, relative to the target's centre.")]
    private float diveAimOffsetY = 0.3f;

    [Header("Animation")]
    [SerializeField] private string[] attackTriggers = { "attack1", "attack2" };
    [SerializeField] private string speedFloat = "speed";

    [Header("Sounds")]
    [SerializeField] private AudioClip attackSound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.7f;

    private Animator anim;
    private EnemyHealth health;
    private float cooldownTimer;
    private bool attacking;
    private bool engaged;
    private int comboIndex;
    private float homeY;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();

        if (patrolScripts == null || patrolScripts.Length == 0)
        {
            var list = new System.Collections.Generic.List<Behaviour>();
            foreach (var mb in GetComponents<MonoBehaviour>())
            {
                if (mb.GetType().Name.Contains("Patrol")) list.Add(mb);
            }
            patrolScripts = list.ToArray();
        }
    }

    private void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.GetComponent<Health>();
        }
        cooldownTimer = attackCooldown * 0.5f;
        homeY = transform.position.y;
    }

    private void OnDisable()
    {
        attacking = false;
        SetEngaged(false);
    }

    private void Update()
    {
        if (target == null || (health != null && health.IsDead)) return;

        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        Vector2 toTarget = target.transform.position - transform.position;
        bool targetAlive = !target.IsDead;
        bool inEngage = targetAlive && Mathf.Abs(toTarget.x) <= engageRange && Mathf.Abs(toTarget.y) <= verticalTolerance;
        bool inAttack = targetAlive && Mathf.Abs(toTarget.x) <= attackRange && Mathf.Abs(toTarget.y) <= verticalTolerance;

        SetEngaged(inEngage || attacking);

        if (engaged) Face(toTarget.x);

        if (diveAtTarget && !attacking && !Mathf.Approximately(transform.position.y, homeY))
        {
            var p = transform.position;
            p.y = Mathf.MoveTowards(p.y, homeY, returnSpeed * Time.deltaTime);
            transform.position = p;
        }

        if (inAttack && !attacking && cooldownTimer <= 0f)
        {
            StartCoroutine(Attack());
        }
    }

    private void SetEngaged(bool value)
    {
        if (engaged == value) return;
        engaged = value;

        if (!stopPatrolWhileEngaged) return;
        foreach (var p in patrolScripts)
        {
            if (p != null && (health == null || !health.IsDead)) p.enabled = !value;
        }
        if (value) AnimatorUtil.SetFloat(anim, speedFloat, 0f);
    }

    private void Face(float dx)
    {
        if (Mathf.Abs(dx) < 0.05f) return;
        float dir = Mathf.Sign(dx) * (spriteFacesRight ? 1f : -1f);
        var s = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(s.x) * dir, s.y, s.z);
    }

    private IEnumerator Attack()
    {
        attacking = true;
        cooldownTimer = attackCooldown;

        if (attackTriggers != null && attackTriggers.Length > 0)
        {
            AnimatorUtil.SetTrigger(anim, attackTriggers[comboIndex % attackTriggers.Length]);
            comboIndex++;
        }
        Sfx.Play(attackSound, soundVolume, 0.1f);

        if (diveAtTarget)
        {
            float t = 0f;
            while (t < hitDelay)
            {
                if (health != null && health.IsDead) { attacking = false; yield break; }
                float aimY = target.transform.position.y + diveAimOffsetY;
                var p = transform.position;
                p.y = Mathf.MoveTowards(p.y, aimY, diveSpeed * Time.deltaTime);
                transform.position = p;
                t += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(hitDelay);
        }

        if (health == null || !health.IsDead)
        {
            Vector2 toTarget = target.transform.position - transform.position;
            if (Mathf.Abs(toTarget.x) <= attackRange * 1.15f && Mathf.Abs(toTarget.y) <= verticalTolerance)
            {
                target.TakeDamage(damage, transform.position);
            }
        }

        attacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f);
        Gizmos.DrawWireCube(transform.position, new Vector3(attackRange * 2f, verticalTolerance * 2f, 0f));
        Gizmos.color = new Color(1f, 0.8f, 0.2f);
        Gizmos.DrawWireCube(transform.position, new Vector3(engageRange * 2f, verticalTolerance * 2f, 0f));
    }
}
