using UnityEngine;
using System.Collections;

public class WaterBalloonTrap : MonoBehaviour
{
    [Header("爆発設定")]
    [SerializeField] private float flashDuration = 2f;
    [SerializeField] private float flashSpeed = 0.1f;
    [SerializeField] private float expandScale = 1.3f;
    [SerializeField] private float expandDuration = 0.3f;

    [Header("参照")]
    [SerializeField] private Renderer balloonRenderer;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private Material flashMaterial; // ★点滅用の白いマテリアル

    [Header("エフェクト設定")]
    [SerializeField] private float explosionEffectScale = 1.5f; // エフェクトの大きさ
    [SerializeField] private float explosionEffectDuration = 3f; // エフェクトが消えるまでの時間

    [Header("スタン判定")]
    [SerializeField] private SphereCollider explosionCollider;
    [SerializeField] private StunType stunType = StunType.Heavy;

    [Header("設置トラップ判定")]
    [SerializeField] private Collider trapCollider;       // 設置中に触れたら反応するCollider（Trigger推奨）
    [SerializeField] private bool disableTrapAfterHit = true; // 触れたら二度と反応しない
    [SerializeField] private bool explodeImmediatelyOnHit = false; // 触れたら即爆発させるか（任意）

    [Header("自動爆発タイマー")]
    [SerializeField] private float autoExplodeDelay = 20f;     // 設置後に爆発するまで
    [SerializeField] private float preFlashTime = 2f;          // 爆発の何秒前からフラッシュ開始

    private Color originalColor;
    private Vector3 originalScale;
    private bool isExploding = false;
    private Material materialInstance;
    private Material originalMaterial; // ★元のマテリアル保存用

    private float explodeAtTime;        // 爆発予定時刻(Time.time基準)
    private bool flashStarted = false;  // フラッシュ開始済みか
    private Coroutine explosionRoutine; // 既存の爆発シーケンス管理（任意）

    void Start()
    {
        if (balloonRenderer == null)
        {
            balloonRenderer = GetComponent<Renderer>();
            if (balloonRenderer == null)
            {
                balloonRenderer = GetComponentInChildren<Renderer>();
            }
        }

        if (balloonRenderer == null)
        {
            Debug.LogError("Rendererが見つかりません!");
            return;
        }

        // 元のマテリアルを保存
        originalMaterial = balloonRenderer.material;
        materialInstance = originalMaterial;

        try
        {
            if (materialInstance.HasProperty("_Color"))
            {
                originalColor = materialInstance.GetColor("_Color");
            }
            else
            {
                originalColor = Color.white;
            }
        }
        catch
        {
            originalColor = Color.white;
        }

        originalScale = transform.localScale;

        if (trapCollider == null)
            trapCollider = GetComponent<Collider>(); // 本体につける想定（無ければ手動でAssign）

        if (trapCollider != null)
            trapCollider.isTrigger = true; // 物理衝突じゃなくTriggerで統一するのが楽

        if (explosionCollider != null)
        {
            explosionCollider.enabled = false;
        }

        // ここで「爆発予定時刻」を決める
        explodeAtTime = Time.time + autoExplodeDelay;
        flashStarted = false;
    }

    private void Update()
    {
        if (isExploding) return;

        // 爆発2秒前になったらフラッシュ開始（1回だけ）
        if (!flashStarted && Time.time >= explodeAtTime - preFlashTime)
        {
            flashStarted = true;

            // フラッシュだけ先行で開始しておく
            StartCoroutine(FlashWhite());
        }

        // 爆発時刻になったら爆発（膨張→爆発はそのまま）
        if (Time.time >= explodeAtTime)
        {
            StartExplosion(); // Expand→Explodeへ
        }
    }


    public void StartExplosion()
    {
        if (isExploding) return;
        isExploding = true;

        if (explosionRoutine != null) StopCoroutine(explosionRoutine);
        explosionRoutine = StartCoroutine(ExplosionSequence());
    }

    private IEnumerator ExplosionSequence()
    {
        // フラッシュはもう走っている想定。ここでは膨張→爆発のみ
        yield return StartCoroutine(ExpandBalloon());
        Explode();
    }

    private IEnumerator FlashWhite()
    {
        float elapsed = 0f;
        bool isWhite = false;
        float nextToggle = 0f;

        while (elapsed < flashDuration)
        {
            // 一定間隔でマテリアルを切り替え
            if (elapsed >= nextToggle)
            {
                if (isWhite)
                {
                    // 元のマテリアルに戻す
                    balloonRenderer.material = originalMaterial;
                }
                else
                {
                    // 白いマテリアルに変更
                    if (flashMaterial != null)
                    {
                        balloonRenderer.material = flashMaterial;
                    }
                    else
                    {
                        Debug.LogWarning("Flash Materialが設定されていません!");
                    }
                }

                isWhite = !isWhite;
                nextToggle = elapsed + flashSpeed;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 最後は白にする
        if (flashMaterial != null)
        {
            balloonRenderer.material = flashMaterial;
        }
    }

    private IEnumerator ExpandBalloon()
    {
        float elapsed = 0f;
        Vector3 targetScale = originalScale * expandScale;

        while (elapsed < expandDuration)
        {
            transform.localScale = Vector3.Lerp(originalScale, targetScale,
                elapsed / expandDuration);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
    }

    private void ExplodeImmediately()
    {
        if (isExploding) return;

        isExploding = true;

        // フラッシュや膨張中なら止める（安全）
        StopAllCoroutines();

        // 見た目を即白にしたいなら（任意）
        if (flashMaterial != null) balloonRenderer.material = flashMaterial;

        Explode();
    }


    private void Explode()
    {
        if (explosionCollider != null)
        {
            explosionCollider.enabled = true;
        }

        if (explosionEffect != null)
        {
            GameObject effect = Instantiate(explosionEffect, transform.position, Quaternion.identity);

            // エフェクトのサイズを変更
            effect.transform.localScale = Vector3.one * explosionEffectScale;

            // 一定時間後にエフェクトを削除
            Destroy(effect, explosionEffectDuration);
        }

        // 少し待ってから削除（Trigger判定のため）
        Destroy(transform.parent.gameObject, 0.2f);
    }

    // ----------------------------
    // Player 判定
    // ----------------------------
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var car = other.GetComponentInParent<CarController>();
        if (car == null) return;

        // ----------------------------
        // 設置中トラップ判定（爆発前）
        // ----------------------------
        if (!isExploding)
        {
            car.SetStun(stunType);

            car.SetStun(stunType);
            ExplodeImmediately();
            return;
        }

        // ----------------------------
        // 爆発中：範囲スタン判定
        // ----------------------------
        car.SetStun(stunType);
    }



    void OnDestroy()
    {
        if (materialInstance != null && materialInstance != originalMaterial)
        {
            Destroy(materialInstance);
        }
    }
}