using UnityEngine;

public class EyePatrol : MonoBehaviour
{
    [Header("Patrol Points")]
    [SerializeField] private Transform leftEdge;
    [SerializeField] private Transform rightEdge;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float idleDuration = 2f;

    [Header("Animation")]
    [SerializeField] private Animator anim;

    private bool movingLeft = true;
    private float idleTimer = 0f;
    private bool isWaiting = false;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (anim == null)
            anim = GetComponent<Animator>();
    }

    private void Update()
    {
        if (leftEdge == null || rightEdge == null)
            return;

        if (isWaiting)
        {
            idleTimer += Time.deltaTime;

            if (anim != null)
                AnimatorUtil.SetFloat(anim, "speed", 0f);

            if (idleTimer >= idleDuration)
            {
                isWaiting = false;
                idleTimer = 0f;
                movingLeft = !movingLeft;
            }

            return;
        }

        Patrol();
    }

    private void Patrol()
    {
        float direction = movingLeft ? -1f : 1f;
        Transform target = movingLeft ? leftEdge : rightEdge;

        transform.position = Vector3.MoveTowards(
            transform.position,
            new Vector3(
                target.position.x,
                transform.position.y,
                transform.position.z
            ),
            moveSpeed * Time.deltaTime
        );

        if (anim != null)
            AnimatorUtil.SetFloat(anim, "speed", moveSpeed);

        transform.localScale = new Vector3(
            Mathf.Abs(originalScale.x) * direction,
            originalScale.y,
            originalScale.z
        );

        if (Mathf.Abs(transform.position.x - target.position.x) < 0.01f)
        {
            isWaiting = true;
            idleTimer = 0f;

            if (anim != null)
                AnimatorUtil.SetFloat(anim, "speed", 0f);
        }
    }

    private void OnDisable()
    {
        if (anim != null)
            AnimatorUtil.SetFloat(anim, "speed", 0f);
    }
}
