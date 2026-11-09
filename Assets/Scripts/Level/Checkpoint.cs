using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField, Tooltip("Respawn offset from this object's position.")]
    private Vector2 respawnOffset = Vector2.zero;
    [SerializeField] private AudioClip activateSound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.6f;

    private bool activated;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated || !other.CompareTag("Player")) return;

        var r = other.GetComponent<respawn>();
        if (r == null) return;

        activated = true;
        r.SetCheckpoint((Vector2)transform.position + respawnOffset);
        Sfx.Play(activateSound, soundVolume);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 3f, 0f));
    }
}
