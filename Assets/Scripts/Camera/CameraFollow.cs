using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform testPlayer;

    [SerializeField] private float smoothTime = 0.3f;

    [Header("Level Bounds (optional)")]
    [SerializeField] private bool clampX = false;
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 70f;

    private Vector3 velocity = Vector3.zero;
    private float fixedY;
    private float fixedZ;

    private void Start()
    {
        fixedY = transform.position.y;
        fixedZ = transform.position.z;
    }

    private void LateUpdate()
    {
        if (testPlayer == null)
        {
            return;
        }

        float x = testPlayer.position.x;
        if (clampX)
        {
            x = Mathf.Clamp(x, minX, maxX);
        }

        Vector3 targetPosition = new Vector3(
            x,
            fixedY,
            fixedZ
        );

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );
    }
}
