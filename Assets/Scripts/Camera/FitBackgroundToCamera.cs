using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class FitBackgroundToCamera : MonoBehaviour
{
    public enum Mode { Fit, Cover }

    [SerializeField] private Camera targetCamera;
    [SerializeField] private Mode mode = Mode.Fit;
    [SerializeField, Tooltip("Distance in front of the camera.")] private float depth = 15f;

    private SpriteRenderer spriteRenderer;

    private void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (targetCamera == null) targetCamera = GetComponentInParent<Camera>();
        if (targetCamera == null) targetCamera = Camera.main;
        Apply();
    }

    private void LateUpdate() => Apply();

    private void Apply()
    {
        if (targetCamera == null || spriteRenderer == null || spriteRenderer.sprite == null || !targetCamera.orthographic) return;

        float viewH = targetCamera.orthographicSize * 2f;
        float viewW = viewH * targetCamera.aspect;
        Vector2 size = spriteRenderer.sprite.bounds.size;

        float sx = viewW / size.x, sy = viewH / size.y;
        float s = mode == Mode.Fit ? Mathf.Min(sx, sy) : Mathf.Max(sx, sy);
        transform.localScale = new Vector3(s, s, 1f);

        Vector3 camPos = targetCamera.transform.position;
        transform.position = new Vector3(camPos.x, camPos.y, camPos.z + depth);
    }
}
