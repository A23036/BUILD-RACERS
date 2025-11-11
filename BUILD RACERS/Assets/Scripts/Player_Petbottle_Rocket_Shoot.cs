using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [Header("ロケット設定")]
    [SerializeField] private GameObject rocketPrefab;

    [Header("発射設定")]
    [SerializeField] private Transform launchPoint;
    [SerializeField] private float forwardOffset = 1.5f;
    [SerializeField] private Vector3 rotationOffset = new Vector3(90f, 0f, 0f);

    [Header("ロケット動作設定")]
    [SerializeField] private float rocketSpeed = 20f;
    [SerializeField] private float rocketLifeTime = 5f;
    [SerializeField] private int maxReflectCount = 3;

    [Header("エフェクト設定（オプション）")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject destroyEffectPrefab;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShootRocket();
        }
    }

    void ShootRocket()
    {
        if (rocketPrefab == null)
        {
            Debug.LogWarning("Rocket Prefabが設定されていません！");
            return;
        }

        // 1. 発射位置を決定
        Vector3 position = launchPoint != null
            ? launchPoint.position
            : transform.position + transform.forward * forwardOffset;

        // 2. 発射時の回転を決定
        Quaternion offsetRotation = Quaternion.Euler(rotationOffset);
        Quaternion finalRotation = transform.rotation * offsetRotation;

        // 3. ロケットを生成
        GameObject rocket = Instantiate(rocketPrefab, position, finalRotation);

        // 4. ロケットに動作スクリプトを追加
        RocketBehavior behavior = rocket.AddComponent<RocketBehavior>();
        behavior.Initialize(rocketSpeed, rocketLifeTime, maxReflectCount, hitEffectPrefab, destroyEffectPrefab);
    }

    // ロケットの動作を制御する内部クラス
    private class RocketBehavior : MonoBehaviour
    {
        private float speed;
        private float lifeTime;
        private int maxReflectCount;
        private int currentReflectCount;
        private Vector3 currentDirection;
        private Rigidbody rb;
        private GameObject hitEffectPrefab;
        private GameObject destroyEffectPrefab;

        public void Initialize(float speed, float lifeTime, int maxReflect, GameObject hitEffect, GameObject destroyEffect)
        {
            this.speed = speed;
            this.lifeTime = lifeTime;
            this.maxReflectCount = maxReflect;
            this.currentReflectCount = maxReflect;
            this.hitEffectPrefab = hitEffect;
            this.destroyEffectPrefab = destroyEffect;

            // 初期の移動方向をローカルの上方向に設定
            currentDirection = transform.up.normalized;

            // Rigidbodyの設定
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // 自動破壊タイマー
            Destroy(gameObject, lifeTime);
        }

        void FixedUpdate()
        {
            // ★ 修正: linearVelocity を使用 ★
            if (rb != null)
            {
                rb.linearVelocity = currentDirection * speed;
            }

            // 進行方向を向く（水平姿勢を維持）
            if (currentDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(currentDirection, Vector3.up);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            // ★ Wallタグのチェック：Wallに当たった場合のみ反射 ★
            if (collision.gameObject.CompareTag("Wall"))
            {
                // 反射回数が残っているか確認
                if (currentReflectCount > 0)
                {
                    PerformReflection(collision);
                }
                else
                {
                    // 反射回数が残っていない場合は破壊
                    Debug.Log("反射回数上限に達しました");
                    DestroyRocket(collision.contacts[0].point);
                }
            }
            else
            {
                // ★ Wall以外に当たったら即座に破壊 ★
                Debug.Log($"{collision.gameObject.name}に命中！ロケット破壊");
                DestroyRocket(collision.contacts[0].point);
            }
        }

        private void PerformReflection(Collision collision)
        {
            // 衝突情報の取得
            ContactPoint contact = collision.contacts[0];
            Vector3 surfaceNormal = contact.normal;

            // めり込み防止のため、少し離れた位置に移動
            //transform.position = contact.point + surfaceNormal * 0.1f;

            // 新しい方向を計算（反射）
            currentDirection = Vector3.Reflect(currentDirection, surfaceNormal).normalized;

            // 即座に回転を更新
            transform.rotation = Quaternion.LookRotation(currentDirection, Vector3.up);

            // ★ 修正: linearVelocity を使用 ★
            if (rb != null)
            {
                rb.linearVelocity = currentDirection * speed;
            }

            // 反射回数を減らす
            currentReflectCount--;

            // 反射エフェクトの生成（オプション）
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, contact.point, Quaternion.LookRotation(surfaceNormal));
            }

            Debug.Log($"壁で反射！ 残り回数: {currentReflectCount}");
        }

        private void DestroyRocket(Vector3 position)
        {
            // 破壊エフェクトの生成（オプション）
            if (destroyEffectPrefab != null)
            {
                Instantiate(destroyEffectPrefab, position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}