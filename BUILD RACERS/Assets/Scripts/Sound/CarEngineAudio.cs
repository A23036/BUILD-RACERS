using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CarEngineAudio : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody rb; // 3DならRigidbody, 2DならRigidbody2Dに差し替え
    [SerializeField] private AudioSource engineSource;

    [Header("Clip")]
    [SerializeField] private AudioClip engineLoopClip;

    [Header("Speed Settings")]
    [SerializeField] private float startSpeed = 3f;   // この速度以上で鳴らす（m/s想定）
    [SerializeField] private float fullSpeed = 20f;  // この速度で最大扱い

    [Header("Pitch/Volume")]
    [SerializeField] private float minPitch = 0.85f;
    [SerializeField] private float maxPitch = 1.6f;
    [SerializeField] private float minVolume = 0.0f;
    [SerializeField] private float maxVolume = 0.8f;

    [Header("Pitch Jitter (Wobble)")]
    [SerializeField] private float jitterAmount = 0.03f;     // 揺らぎ量（±） 0.01〜0.05推奨
    [SerializeField] private float jitterUpdateMin = 0.15f;  // 更新間隔（最小）
    [SerializeField] private float jitterUpdateMax = 0.35f;  // 更新間隔（最大）
    [SerializeField] private float jitterSmooth = 6f;        // 揺らぎ追従の滑らかさ

    private float jitterTarget = 0f;
    private float jitterValue = 0f;
    private float jitterTimer = 0f;

    [Header("Smoothing")]
    [SerializeField] private float pitchSmooth = 10f;
    [SerializeField] private float volumeSmooth = 10f;

    [Header("Start/Stop Fade")]
    [SerializeField] private float fadeTime = 0.12f;

    private float targetPitch;
    private float targetVolume;

    private void Reset()
    {
        engineSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (engineSource == null) engineSource = GetComponent<AudioSource>();

        engineSource.playOnAwake = false;
        engineSource.loop = true;
        engineSource.spatialBlend = 1f;
        engineSource.clip = engineLoopClip;

        // 起動時は無音
        engineSource.volume = 0f;
    }

    private void Update()
    {
        float speed = GetSpeed();

        // 速度を 0..1 に正規化
        float t = Mathf.InverseLerp(startSpeed, fullSpeed, speed);
        t = Mathf.Clamp01(t);

        bool shouldPlay = speed >= startSpeed;

        // 目標値を決める
        targetPitch = Mathf.Lerp(minPitch, maxPitch, t);
        targetVolume = Mathf.Lerp(minVolume, maxVolume, t);

        // 再生/停止（フェード）
        if (shouldPlay)
        {
            if (!engineSource.isPlaying && engineLoopClip != null)
                engineSource.Play();

            // フェードイン目標に
        }
        else
        {
            // 目標音量を0に寄せて、完全に0になったら停止
            targetVolume = 0f;
            if (engineSource.isPlaying && engineSource.volume <= 0.001f)
                engineSource.Stop();
        }

        // pitch/volume を滑らかに追従
        float dt = Time.deltaTime;
        engineSource.pitch = Mathf.Lerp(engineSource.pitch, targetPitch, 1f - Mathf.Exp(-pitchSmooth * dt));

        float volSmooth = (fadeTime > 0f) ? (1f / fadeTime) : volumeSmooth;
        engineSource.volume = Mathf.Lerp(engineSource.volume, targetVolume, 1f - Mathf.Exp(-volSmooth * dt));
    }

    private float GetSpeed()
    {
        if (rb != null)
            return rb.linearVelocity.magnitude;

        // 速度取得先が別ならここを書き換え（例: CarControllerのSpeed）
        return 0f;
    }
}
