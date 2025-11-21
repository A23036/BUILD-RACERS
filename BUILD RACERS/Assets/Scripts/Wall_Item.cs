using UnityEngine;

public class BreakableWall : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("壁が消えるまでの時間（秒）")]
    public float disappearTime = 3f;

    [Tooltip("当たり判定を無効化するまでの時間（秒）")]
    public float disableCollisionTime = 0.5f;

    [Tooltip("判定するPlayerのタグ")]
    public string playerTag = "Player";

    [Header("跳ね返し設定")]
    [Tooltip("跳ね返す力の強さ")]
    public float bounceForce = 10f;

    [Tooltip("跳ね返す方向（0=衝突方向, 1=上方向のみ）")]
    [Range(0f, 1f)]
    public float upwardBias = 0.5f;

    [Header("エフェクト設定")]
    [Tooltip("壁が消える時に再生するエフェクトのPrefab")]
    public GameObject destroyEffectPrefab;

    private Rigidbody rb;
    private bool hasTriggered = false;

    void Start()
    {
        // Rigidbodyコンポーネントを取得または追加
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // 最初は物理演算を無効化（kinematicにする）
        rb.isKinematic = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        // すでに発動済みなら何もしない
        if (hasTriggered) return;

        // Playerタグのオブジェクトと衝突したか確認
        if (collision.gameObject.CompareTag(playerTag))
        {
            // プレイヤーを跳ね返す
            BouncePlayer(collision);

            // 物理演算を有効化
            ActivatePhysics();
        }
    }

    void ActivatePhysics()
    {
        hasTriggered = true;

        // 物理演算を有効化
        rb.isKinematic = false;

        // 指定時間後に当たり判定を無効化
        //Invoke(nameof(DisableCollision), disableCollisionTime);

        // 指定時間後に壁を消す（エフェクト付き）
        Invoke(nameof(DestroyWithEffect), disappearTime);

        Debug.Log($"{gameObject.name} の物理演算が有効化されました。{disappearTime}秒後に消えます。");
    }

    void BouncePlayer(Collision collision)
    {
        // プレイヤーのRigidbodyを取得
        Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
        if (playerRb == null) return;

        // 衝突した点の法線方向を取得
        Vector3 normal = collision.contacts[0].normal;

        // 上方向バイアスを適用
        Vector3 bounceDirection = Vector3.Lerp(normal, Vector3.up, upwardBias).normalized;

        // 跳ね返す力を加える
        playerRb.linearVelocity = Vector3.zero; // 一度速度をリセット
        playerRb.AddForce(bounceDirection * bounceForce, ForceMode.Impulse);

        Debug.Log($"プレイヤーを {bounceDirection} 方向に跳ね返しました。");
    }

    void DisableCollision()
    {
        // すべてのColliderを無効化
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // 子オブジェクトのColliderも無効化
        Collider[] childColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in childColliders)
        {
            col.enabled = false;
        }

        Debug.Log($"{gameObject.name} の当たり判定が無効化されました。");
    }

    void DestroyWithEffect()
    {
        // エフェクトが設定されている場合は再生
        if (destroyEffectPrefab != null)
        {
            // 壁の位置にエフェクトを生成
            GameObject effect = Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);

            // エフェクトにParticleSystemがある場合、自動削除を設定
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                // ParticleSystemがない場合は5秒後に削除
                Destroy(effect, 5f);
            }

            Debug.Log($"{gameObject.name} のエフェクトを再生しました。");
        }

        // 壁を削除
        Destroy(gameObject);
    }
}