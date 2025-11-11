using UnityEngine;
using System.Collections; // コルーチンを使用するために追加

public class ItemBoxController : MonoBehaviour
{
    [Header("エフェクト設定")]
    [SerializeField]
    private GameObject breakEffectPrefab; // 破壊時に再生するパーティクルエフェクトのプレハブ

    [Header("復活設定")]
    [SerializeField]
    private bool enableRespawn = true; // 復活機能を有効にするか
    [SerializeField]
    private float respawnTime = 5f; // 復活するまでの時間（秒）
    [SerializeField]
    private GameObject visualObject; // 見た目のオブジェクト（子オブジェクトを指定）
    [SerializeField]
    private float respawnAnimationDuration = 0.5f; // 復活アニメーションの時間（秒）
    [SerializeField]
    private float respawnStartScale = 0.0f; // 復活開始時のスケール（0なら完全に縮小）

    private Collider boxCollider; // このボックスのコライダー
    private bool isDestroyed = false; // 破壊されているかどうか
    private bool isRespawning = false; // 復活アニメーション中かどうか
    private float respawnTimer = 0f; // 復活アニメーションのタイマー

    [Header("アイテムボックスのスケール")]
    // スケールが変化する速さ
    public float scaleSpeed = 0.3f;
    // 最小スケール（例: 0.5なら元の半分の大きさ）
    public float minScale = 0.95f;
    // 最大スケール（例: 1.5なら元の1.5倍の大きさ）
    public float maxScale = 1.05f;
    private Vector3 originalScale;

    void Start()
    {
        // オブジェクトの元のスケールを保存しておきます
        originalScale = transform.localScale;

        // コライダーを取得
        boxCollider = GetComponent<Collider>();

        // visualObjectが設定されていない場合、自分自身を使用
        if (visualObject == null)
        {
            visualObject = gameObject;
        }
    }

    void Update()
    {
        // 復活アニメーション中の処理
        if (isRespawning)
        {
            respawnTimer += Time.deltaTime;
            float progress = respawnTimer / respawnAnimationDuration;

            if (progress >= 1.0f)
            {
                // アニメーション終了
                isRespawning = false;
                transform.localScale = originalScale;
            }
            else
            {
                // イージング関数を使って滑らかに拡大
                float easedProgress = EaseOutBack(progress);
                float currentScale = Mathf.Lerp(respawnStartScale, 1.0f, easedProgress);
                transform.localScale = originalScale * currentScale;
            }
            return;
        }

        // 破壊されている間はスケールアニメーションを停止
        if (isDestroyed) return;

        // 1. ピンポン運動の値を取得
        float pingPongValue = Mathf.PingPong(Time.time * scaleSpeed, 1.0f);

        // 2. 0～1.0の値を minScale と maxScale の間に変換
        float scale = Mathf.Lerp(minScale, maxScale, pingPongValue);

        // 3. 計算したスケールをオブジェクトに適用
        transform.localScale = originalScale * scale;
    }

    private void OnTriggerEnter(Collider other)
    {
        // すでに破壊されている、または復活アニメーション中は無視
        if (isDestroyed || isRespawning) return;

        if (other.gameObject.CompareTag("Player"))
        {
            // --- アイテムボックスが破壊されるときの処理 ---
            // 1. アイテムを渡す処理をここに記述（あれば）

            // 破壊状態にして、見た目とコライダーを非表示/無効化
            HideBox();

            // コルーチンを開始し、エフェクト再生後に次の処理を行う
            StartCoroutine(HandleBoxBreak());
        }
    }

    /// <summary>
    /// アイテムボックスが破壊された後の処理（エフェクト再生、復活待機または完全破棄）
    /// </summary>
    private IEnumerator HandleBoxBreak()
    {
        // 2. 破壊エフェクトを生成し、終了まで待機
        if (breakEffectPrefab != null)
        {
            // エフェクトを生成し、インスタンスを保持
            GameObject effectInstance = Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);

            float effectDuration = 0f;
            ParticleSystem ps = effectInstance.GetComponent<ParticleSystem>();

            if (ps != null)
            {
                // エフェクトが完全に終わるまでの時間を計算 (Duration + Start Lifetime)
                // ParticleSystemの設定によっては constantMax でなく他の値を使う必要がある場合があります。
                effectDuration = ps.main.duration + ps.main.startLifetime.constantMax;
            }

            // エフェクトの再生時間分待機
            if (effectDuration > 0f)
            {
                yield return new WaitForSeconds(effectDuration);
            }
            else
            {
                // 再生時間が不明な場合は最低限の待機時間
                yield return new WaitForSeconds(1.5f);
            }

            // エフェクトオブジェクトを破棄（自動で消えない設定のエフェクトでもこれで確実に消える）
            if (effectInstance != null)
            {
                Destroy(effectInstance);
            }
        }
        else
        {
            // エフェクトがない場合は次のフレームまで待機
            yield return null;
        }

        // 3. 復活機能の処理
        if (enableRespawn)
        {
            // 復活時間分待機
            yield return new WaitForSeconds(respawnTime);
            RespawnBox(); // 復活処理を呼び出す
        }
        else
        {
            // 復活機能が無効なら完全に破壊
            Destroy(gameObject);
        }
    }


    /// <summary>
    /// アイテムボックスを非表示にする
    /// </summary>
    private void HideBox()
    {
        isDestroyed = true;

        // 見た目を非表示
        if (visualObject != null)
        {
            visualObject.SetActive(false);
        }

        // コライダーを無効化（再び取得できないようにする）
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }
    }

    /// <summary>
    /// アイテムボックスを復活させる
    /// </summary>
    private void RespawnBox()
    {
        isDestroyed = false;
        isRespawning = true;
        respawnTimer = 0f;

        // 見た目を再表示
        if (visualObject != null)
        {
            visualObject.SetActive(true);
        }

        // コライダーを有効化
        if (boxCollider != null)
        {
            boxCollider.enabled = true;
        }

        // 初期スケールを縮小状態に設定
        transform.localScale = originalScale * respawnStartScale;
    }

    /// <summary>
    /// イージング関数：EaseOutBack（少しバウンドする感じ）
    /// </summary>
    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // Unity Editorで停止した時にInvokeとコルーチンをキャンセル
    private void OnDisable()
    {
        CancelInvoke();
        StopAllCoroutines();
    }
}