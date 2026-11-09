using UnityEngine;

public static class Sfx
{
    private static AudioSource source;

    private static AudioSource Source
    {
        get
        {
            if (source == null)
            {
                var go = new GameObject("[Sfx]");
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
            }
            return source;
        }
    }

    public static void Play(AudioClip clip, float volume = 1f, float pitchVariation = 0f)
    {
        if (clip == null) return;

        var s = Source;
        s.pitch = pitchVariation > 0f ? 1f + Random.Range(-pitchVariation, pitchVariation) : 1f;
        s.PlayOneShot(clip, volume);
    }
}
