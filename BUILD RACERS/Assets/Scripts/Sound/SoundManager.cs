using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;
    [SerializeField] private AudioSource loopSeSource;

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string paramBgm = "BGM_Vol"; // Exposed Parameters名と一致
    [SerializeField] private string paramSe = "SE_Vol";  // Exposed Parameters名と一致

    [Header("Settings")]
    [SerializeField] private float defaultFadeTime = 1.0f;

    // 保存キー（OptionSceneと合わせる）
    private const string KEY_BGM = "Volume_BGM_20";
    private const string KEY_SE = "Volume_SE_20";
    private const int STEP_MAX = 20;

    private int bgmStep = STEP_MAX;
    private int seStep = STEP_MAX;

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

        if (bgmSource != null) { bgmSource.loop = true; bgmSource.playOnAwake = false; }
        if (seSource != null) { seSource.loop = false; seSource.playOnAwake = false; }
        if (loopSeSource != null) { loopSeSource.loop = false; loopSeSource.playOnAwake = false; }

        // 保存値ロード（0〜20）
        bgmStep = Mathf.Clamp(PlayerPrefs.GetInt(KEY_BGM, STEP_MAX), 0, STEP_MAX);
        seStep = Mathf.Clamp(PlayerPrefs.GetInt(KEY_SE, STEP_MAX), 0, STEP_MAX);

        // Mixerへ反映
        ApplyMixerVolumes(save: false);
    }

    // --------------------
    // Volume API（OptionSceneはこれを呼ぶのが楽）
    // --------------------
    public void SetBgmStep(int step, bool save = true)
    {
        bgmStep = Mathf.Clamp(step, 0, STEP_MAX);
        ApplyMixerVolumes(save);
    }

    public void SetSeStep(int step, bool save = true)
    {
        seStep = Mathf.Clamp(step, 0, STEP_MAX);
        ApplyMixerVolumes(save);
    }

    // 0..1 で触りたい場合用
    public void SetBGMVolume01(float volume01, bool save = true)
    {
        SetBgmStep(Mathf.RoundToInt(Mathf.Clamp01(volume01) * STEP_MAX), save);
    }

    public void SetSEVolume01(float volume01, bool save = true)
    {
        SetSeStep(Mathf.RoundToInt(Mathf.Clamp01(volume01) * STEP_MAX), save);
    }

    public float GetBGMVolume01() => bgmStep / (float)STEP_MAX;
    public float GetSEVolume01() => seStep / (float)STEP_MAX;

    private void ApplyMixerVolumes(bool save)
    {
        if (mixer != null)
        {
            mixer.SetFloat(paramBgm, ToDecibel(GetBGMVolume01()));
            mixer.SetFloat(paramSe, ToDecibel(GetSEVolume01()));
        }

        if (save)
        {
            PlayerPrefs.SetInt(KEY_BGM, bgmStep);
            PlayerPrefs.SetInt(KEY_SE, seStep);
            PlayerPrefs.Save();
        }
    }

    private float ToDecibel(float value01)
    {
        if (value01 <= 0.0001f) return -80f;
        return Mathf.Log10(value01) * 20f;
    }

    // --------------------
    // BGM
    // --------------------
    public void PlayBGM(AudioClip clip, float fadeTime = -1f, bool keepIfSame = true)
    {
        if (clip == null || bgmSource == null) return;
        if (keepIfSame && bgmSource.isPlaying && bgmSource.clip == clip) return;

        if (fadeTime < 0f) fadeTime = defaultFadeTime;

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

    // フェードは AudioSource.volume で 0→1（マスターはMixer）
    private IEnumerator FadeSwapBGM(AudioClip next, float fadeTime)
    {
        if (bgmSource.isPlaying && bgmSource.volume > 0f)
            yield return FadeVolume(bgmSource, 0f, fadeTime);

        bgmSource.clip = next;
        bgmSource.volume = 0f;
        bgmSource.Play();

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
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(start, target, t / time);
            yield return null;
        }
        src.volume = target;
    }

    // --------------------
    // SE
    // --------------------
    // マスターはMixerが掛けるので volumeScale だけ
    public void PlaySE(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || seSource == null) return;
        seSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlayLoopSE(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || loopSeSource == null) return;

        loopSeSource.clip = clip;
        loopSeSource.loop = true;
        loopSeSource.volume = Mathf.Clamp01(volumeScale);

        if (!loopSeSource.isPlaying)
            loopSeSource.Play();
    }

    public void StopLoopSE()
    {
        if (loopSeSource != null && loopSeSource.isPlaying)
            loopSeSource.Stop();
    }
}
