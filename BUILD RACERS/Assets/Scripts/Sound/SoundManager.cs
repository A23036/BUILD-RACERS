using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;

    [Header("Settings")]
    [SerializeField] private float defaultFadeTime = 1.0f;

    private Coroutine bgmFadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 念のため最低限の初期化
        if (bgmSource != null)
        {
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        if (seSource != null)
        {
            seSource.loop = false;
            seSource.playOnAwake = false;
        }
    }

    // --------------------
    // BGM
    // --------------------
    public void PlayBGM(AudioClip clip, float fadeTime = -1f, bool keepIfSame = true)
    {
        if (clip == null || bgmSource == null) return;

        if (keepIfSame && bgmSource.isPlaying && bgmSource.clip == clip) return;

        if (fadeTime < 0f) fadeTime = defaultFadeTime;

        // フェードしながら切り替え（クロスフェードではなく、いったん下げて差し替えて上げる）
        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(FadeSwapBGM(clip, fadeTime));
    }

    public void StopBGM(float fadeTime = -1f)
    {
        if (bgmSource == null) return;
        if (fadeTime < 0f) fadeTime = defaultFadeTime;

        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(FadeOutStop(fadeTime));
    }

    public void SetBGMVolume(float volume01)
    {
        // AudioSource直 or MixerどちらでもOK
        volume01 = Mathf.Clamp01(volume01);

        if (bgmSource != null)
        {
            bgmSource.volume = volume01;
        }
    }

    // --------------------
    // SE
    // --------------------
    public void PlaySE(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || seSource == null) return;
        seSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void SetSEVolume(float volume01)
    {
        volume01 = Mathf.Clamp01(volume01);

        if (seSource != null)
        {
            seSource.volume = volume01;
        }
    }

    // --------------------
    // Coroutines
    // --------------------
    private IEnumerator FadeSwapBGM(AudioClip next, float fadeTime)
    {
        // 1) 今鳴ってたらフェードアウト
        if (bgmSource.isPlaying && bgmSource.volume > 0f)
        {
            yield return FadeVolume(bgmSource, 0f, fadeTime);
        }

        // 2) 差し替え＆再生
        bgmSource.clip = next;
        bgmSource.Play();

        // 3) フェードイン（元の最大音量を1として扱う）
        yield return FadeVolume(bgmSource, 1f, fadeTime);
    }

    private IEnumerator FadeOutStop(float fadeTime)
    {
        if (!bgmSource.isPlaying) yield break;

        yield return FadeVolume(bgmSource, 0f, fadeTime);
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    private IEnumerator FadeVolume(AudioSource src, float target, float time)
    {
        float start = src.volume;
        float t = 0f;

        if (time <= 0f)
        {
            src.volume = target;
            yield break;
        }

        while (t < time)
        {
            t += Time.unscaledDeltaTime; // ポーズ中でもフェードしたいなら unscaled
            src.volume = Mathf.Lerp(start, target, t / time);
            yield return null;
        }
        src.volume = target;
    }

    private float ToDecibel(float value01)
    {
        // 0 -> -80dB くらいに落とす（無音）
        if (value01 <= 0.0001f) return -80f;
        return Mathf.Log10(value01) * 20f;
    }
}
