using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private const float FadeDurationSeconds = 2f;

    private AudioSource musicSource;
    private readonly List<AudioClip> musicClips = new List<AudioClip>();

    private Coroutine playbackCoroutine;
    private Coroutine fadeCoroutine;

    private bool stopRequested;
    private int lastTrackIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureAudioSource();
        LoadMusicClips();
    }

    public void StartGameplayMusic()
    {
        if (musicClips.Count == 0)
        {
            return;
        }
        stopRequested = false;
        if (playbackCoroutine == null)
        {
            playbackCoroutine = StartCoroutine(PlaybackLoop());
        }
    }

    public void StopImmediate()
    {
        stopRequested = true;
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            musicSource.volume = 0f;
        }
    }

    private void EnsureAudioSource()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.GetComponent<AudioSource>();
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }
            musicSource.playOnAwake = false;
            musicSource.loop = false;
            musicSource.spatialBlend = 0f; // 2D
            musicSource.volume = 0f;       // we control gain via fades 0..1, multiplied by global AudioListener volume
        }
    }

    private void LoadMusicClips()
    {
        musicClips.Clear();
        TryLoadIntoList("Soundtracks", musicClips);
        TryLoadIntoList("Music", musicClips);
        TryLoadIntoList("Audio/Soundtracks", musicClips);
        TryLoadIntoList("Audio/Music", musicClips);
        TryLoadIntoList("Audio", musicClips);
        // Deduplicate in case of overlaps
        var set = new HashSet<AudioClip>(musicClips);
        musicClips.Clear();
        musicClips.AddRange(set);
    }

    private static void TryLoadIntoList(string resourcesPath, List<AudioClip> destination)
    {
        var found = Resources.LoadAll<AudioClip>(resourcesPath);
        if (found != null && found.Length > 0)
        {
            destination.AddRange(found);
        }
    }

    private IEnumerator PlaybackLoop()
    {
        while (!stopRequested)
        {
            var clip = ChooseNextRandomClip();
            if (clip == null)
            {
                yield break;
            }

            yield return PlaySingleClipWithFades(clip);

            if (stopRequested)
            {
                yield break;
            }
        }
    }

    private AudioClip ChooseNextRandomClip()
    {
        if (musicClips.Count == 0) return null;
        if (musicClips.Count == 1)
        {
            lastTrackIndex = 0;
            return musicClips[0];
        }
        for (int safety = 0; safety < 20; safety++)
        {
            int idx = Random.Range(0, musicClips.Count);
            if (idx != lastTrackIndex)
            {
                lastTrackIndex = idx;
                return musicClips[idx];
            }
        }
        // Fallback if random kept choosing the same
        lastTrackIndex = (lastTrackIndex + 1) % musicClips.Count;
        return musicClips[lastTrackIndex];
    }

    private IEnumerator PlaySingleClipWithFades(AudioClip clip)
    {
        EnsureAudioSource();
        musicSource.clip = clip;
        musicSource.volume = 0f;
        musicSource.Play();

        if (stopRequested) yield break;
        yield return FadeToVolume(1f, FadeDurationSeconds);

        if (stopRequested)
        {
            yield break;
        }

        float clipLength = clip.length;
        float fadeDur = Mathf.Max(0f, FadeDurationSeconds);

        // Wait until it's time to start fading out
        while (!stopRequested && musicSource.isPlaying)
        {
            float timeRemaining = Mathf.Max(0f, clipLength - musicSource.time);
            if (timeRemaining <= fadeDur)
            {
                break;
            }
            yield return null;
        }

        if (!stopRequested && musicSource.isPlaying)
        {
            yield return FadeToVolume(0f, fadeDur);
        }

        // Let the clip finish naturally if still playing
        while (!stopRequested && musicSource.isPlaying)
        {
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = null;
        musicSource.volume = 0f;
    }

    private IEnumerator FadeToVolume(float target01, float duration)
    {
        if (duration <= 0f)
        {
            musicSource.volume = Mathf.Clamp01(target01);
            yield break;
        }
        float start = musicSource.volume;
        float t = 0f;
        // Replace any existing fade coroutine reference with this one
        fadeCoroutine = StartCoroutine(FadeRoutine());
        yield return fadeCoroutine;
        fadeCoroutine = null;

        IEnumerator FadeRoutine()
        {
            while (t < duration && !stopRequested)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                float v = Mathf.Lerp(start, Mathf.Clamp01(target01), u);
                musicSource.volume = v;
                yield return null;
            }
            if (!stopRequested)
            {
                musicSource.volume = Mathf.Clamp01(target01);
            }
        }
    }
}
