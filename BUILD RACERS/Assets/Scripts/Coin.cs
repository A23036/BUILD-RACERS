using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    // === 公開変数 (Inspectorで設定) ===
    public Rigidbody rb;
    public float getRotateSpeed = 720f;
    public float rotateSpeed = 180f;
    public float respawnTime = 10f;
    public float scaleUpSpeed = 5f;

    [Header("エフェクト設定")]
    public ParticleSystem getEffect; // 取得時のパーティクルエフェクト
    public AudioClip getCoinSound;   // 取得時の効果音
    public GameObject effectPrefab;  // エフェクトのPrefab（設定した場合）

    // === プライベート変数 ===
    public bool get;
    public bool isCnt;

    private float gettime;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 originalScale;
    private MeshRenderer meshRenderer;
    private Collider coinCollider;
    private AudioSource audioSource;

    // 全コインで共有するY回転角（度）
    private static float s_sharedY = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        originalScale = transform.localScale;

        meshRenderer = GetComponent<MeshRenderer>();
        coinCollider = GetComponent<Collider>();

        // AudioSourceを取得または追加
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && getCoinSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 起動時に共有Y角を反映
        var e = initialRotation.eulerAngles;
        transform.rotation = Quaternion.Euler(e.x, s_sharedY, e.z);
    }

    void Update()
    {
        if (get == true)
        {
            // ゲット時の回転とアニメーション（共有角度には影響を与えない）
            float delta = getRotateSpeed * Time.deltaTime;
            transform.Rotate(0f, delta, 0f);

            gettime += Time.deltaTime;

            if (gettime > 0f && rb.isKinematic == false)
            {
                rb.isKinematic = true;
            }

            if (gettime > 0.3f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 0.5f);
            }

            if (gettime > 0.5f)
            {
                // 非表示に切り替える直前に共有角度に同期させる（アニメーション中は切り離し）
                var e = initialRotation.eulerAngles;
                transform.rotation = Quaternion.Euler(e.x, s_sharedY, e.z);

                // 見えなくする＆当たり判定をオフ
                meshRenderer.enabled = false;
                coinCollider.enabled = false;
                rb.isKinematic = true;

                // Coroutineで復活
                StartCoroutine(RespawnCoroutine());

                // getフラグをリセット（Coroutine実行を1回だけにする）
                get = false;
                isCnt = false;
                gettime = 0f;
            }
        }
        else if (meshRenderer.enabled) // 通常時（見えている状態）
        {
            // 復活後の拡大アニメーションと通常回転（共有角を使用して全コイン同期）
            if (transform.localScale.magnitude < originalScale.magnitude)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * scaleUpSpeed);
            }

            // 共有角を増加させる
            float delta = rotateSpeed * Time.deltaTime;
            s_sharedY += delta;
            s_sharedY = Mathf.Repeat(s_sharedY, 360f);

            // 各コインのX/Zは初期値を保持しつつYは共有角で合わせる
            var e = initialRotation.eulerAngles;
            transform.rotation = Quaternion.Euler(e.x, s_sharedY, e.z);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (get == false && meshRenderer.enabled)
            {
                get = true;
                gettime = 0f;
                rb.isKinematic = false;
                rb.AddForce(Vector3.up * 5, ForceMode.Impulse);

                // === エフェクトを再生 ===
                PlayGetEffect();
            }
        }
    }

    // エフェクト再生メソッド
    void PlayGetEffect()
    {
        // パーティクルエフェクトを再生
        if (getEffect != null)
        {
            getEffect.Play();
        }

        // Prefabからエフェクトを生成
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 3f); // 3秒後に自動削除
        }

        // 効果音を再生
        if (getCoinSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(getCoinSound);
        }
    }

    IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnTime);
        Respawn();
    }

    public void Respawn()
    {
        // 初期化
        transform.localScale = Vector3.zero;
        transform.position = initialPosition;

        // 初期のX/Z回転は保持しつつ、Yは共有角で復元する
        var e = initialRotation.eulerAngles;
        transform.rotation = Quaternion.Euler(e.x, s_sharedY, e.z);

        rb.isKinematic = true;

        // 見える状態に戻す
        meshRenderer.enabled = true;
        coinCollider.enabled = true;
    }
}