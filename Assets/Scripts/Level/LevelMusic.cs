using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LevelMusic : MonoBehaviour
{
    [SerializeField] private AudioClip music;
    [SerializeField, Range(0f, 1f)] private float volume = 0.65f;
    [SerializeField] private float fadeInTime = 2f;
    [SerializeField, Tooltip("Lower the music while the game is paused.")]
    private bool duckWhenPaused = true;
    [SerializeField, Range(0f, 1f)] private float pausedVolumeScale = 0.4f;

    private AudioSource source;
    private float fade;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
    }

    private void Start()
    {
        if (music == null) return;
        source.clip = music;
        source.volume = 0f;
        source.Play();
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        fade = 0f;
        while (fade < 1f)
        {
            fade = fadeInTime > 0f ? fade + Time.unscaledDeltaTime / fadeInTime : 1f;
            yield return null;
        }
        fade = 1f;
    }

    private void Update()
    {
        if (!source.isPlaying) return;
        float scale = duckWhenPaused && Time.timeScale <= 0f ? pausedVolumeScale : 1f;
        source.volume = volume * Mathf.Clamp01(fade) * scale;
    }
}
