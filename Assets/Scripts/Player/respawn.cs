using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class respawn : MonoBehaviour
{
    [Header("Lives")]
    [SerializeField, Min(1)] private int maxLives = 3;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 1.5f;
    [SerializeField, Tooltip("Optional. If empty the player respawns where the level started.")]
    private Transform respawnPoint;

    [Header("Death Screen")]
    [SerializeField] private float deathScreenDelay = 1.5f;

    [Header("Fall Death")]
    [SerializeField] private bool killWhenFallingOffMap = true;
    [SerializeField] private float fallDeathY = -15f;

    [Header("Sounds")]
    [SerializeField] private AudioClip respawnSound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.8f;

    public event Action<int, int> LivesChanged;

    public int LivesLeft { get; private set; }
    public int MaxLives => maxLives;

    private Health playerHealth;
    private Vector3 initialPosition;

    private void Awake()
    {
        playerHealth = GetComponent<Health>();
        initialPosition = respawnPoint != null ? respawnPoint.position : transform.position;
        LivesLeft = maxLives;
    }

    private void OnEnable()
    {
        playerHealth.onDied.AddListener(OnPlayerDied);
    }

    private void OnDisable()
    {
        playerHealth.onDied.RemoveListener(OnPlayerDied);
    }

    private void Start()
    {
        LivesChanged?.Invoke(LivesLeft, maxLives);
    }

    private void Update()
    {
        if (killWhenFallingOffMap && !playerHealth.IsDead && transform.position.y < fallDeathY)
        {
            playerHealth.Kill();
        }
    }

    public void SetCheckpoint(Vector3 position)
    {
        initialPosition = position;
    }

    private void OnPlayerDied()
    {
        LivesLeft = Mathf.Max(0, LivesLeft - 1);
        LivesChanged?.Invoke(LivesLeft, maxLives);

        StartCoroutine(LivesLeft > 0 ? RespawnAfterDelay() : DeathScreenAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        CheckRespawn();
    }

    private IEnumerator DeathScreenAfterDelay()
    {
        yield return new WaitForSeconds(deathScreenDelay);

        if (GameUIController.Instance != null)
        {
            GameUIController.Instance.ShowDeathScreen();
        }
        else
        {
            Debug.LogWarning("[respawn] No GameUIController in scene, restarting level.");
            SceneLoader.ReloadCurrentScene();
        }
    }

    public void CheckRespawn()
    {
        transform.position = initialPosition;
        playerHealth.Respawn();
        Sfx.Play(respawnSound, soundVolume);
    }
}
