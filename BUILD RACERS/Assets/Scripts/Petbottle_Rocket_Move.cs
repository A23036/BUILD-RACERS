using UnityEngine;

public class RocketMove : MonoBehaviour
{
    // Inspectorで速度を設定できるように public にします
    public float speed = 20f;
    // ロケットが発射されてから自動的に消滅するまでの時間
    public float lifeTime = 5f;

    [Header("Reflect Settings")]
    [Tooltip("ロケットが反射できる最大回数")]
    public int maxReflectCount = 3;
    private int currentReflectCount;
    private Vector3 currentDirection; // 現在の移動方向を格納する変数

    [Header("Height Maintenance Settings")]
    [Tooltip("地面から維持したい高さ")]
    public float targetHeight = 2f;
    [Tooltip("高さ調整の強度（大きいほど素早く調整）")]
    public float heightAdjustSpeed = 5f;
    [Tooltip("Raycastの最大距離")]
    public float raycastDistance = 10f;
    [Tooltip("地面として認識するレイヤー")]
    public LayerMask groundLayer = -1; // -1は全てのレイヤー

    [Header("Effect Settings")]
    [Tooltip("破壊時に再生するエフェクトのPrefab")]
    public GameObject destroyEffectPrefab;
    [Tooltip("エフェクトが自動で消えるまでの時間")]
    public float effectLifeTime = 2f;

    void Start()
    {
        // 破壊タイマーを開始
        Destroy(gameObject, lifeTime);
        // 初期の反射回数を設定
        currentReflectCount = maxReflectCount;
        // 初期の移動方向をローカルの上方向（Z軸）に設定
        currentDirection = transform.forward;
    }

    void Update()
    {
        // 高さ維持処理
        MaintainHeight();

        // 毎フレーム、currentDirectionに基づいて移動します
        transform.Translate(currentDirection * speed * Time.deltaTime, Space.World);

        // 水平姿勢を維持しつつ、進行方向を向く
        if (currentDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(currentDirection, Vector3.up);
        }
    }

    // ★ 高さ維持処理 ★
    void MaintainHeight()
    {
        RaycastHit hit;
        // 下方向にRayを飛ばす
        if (Physics.Raycast(transform.position, Vector3.down, out hit, raycastDistance, groundLayer))
        {
            float currentHeight = hit.distance;
            float heightDifference = targetHeight - currentHeight;

            // 高さの差に応じて上下に移動
            Vector3 newPosition = transform.position;
            newPosition.y += heightDifference * heightAdjustSpeed * Time.deltaTime;
            transform.position = newPosition;

            // デバッグ用：Rayを可視化（Sceneビューで確認可能）
            Debug.DrawRay(transform.position, Vector3.down * hit.distance, Color.green);
        }
        else
        {
            // 地面が検出されない場合（高すぎる、または地面がない）
            Debug.DrawRay(transform.position, Vector3.down * raycastDistance, Color.red);
        }
    }

    // ★ エフェクト再生処理 ★
    void PlayDestroyEffect()
    {
        if (destroyEffectPrefab != null)
        {
            // エフェクトを衝突位置に生成
            GameObject effect = Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
            // エフェクトを一定時間後に自動削除
            Destroy(effect, effectLifeTime);
        }
    }

    // ★ 衝突処理 ★
    void OnCollisionEnter(Collision collision)
    {
        // ★ Wallタグの壁に当たった場合のみ反射 ★
        if (collision.gameObject.CompareTag("Wall"))
        {
            // 反射回数が残っているか確認
            if (currentReflectCount > 0)
            {
                Vector3 incomingVector = currentDirection;
                Vector3 surfaceNormal = collision.contacts[0].normal;
                // 新しい方向を計算
                currentDirection = Vector3.Reflect(incomingVector, surfaceNormal).normalized;
                // 衝突時に即座に回転を更新
                transform.rotation = Quaternion.LookRotation(currentDirection, Vector3.up);
                // 反射回数を減らす
                currentReflectCount--;
            }
            else
            {
                // 反射回数が残っていない場合、ロケットを破壊する
                Destroy(gameObject);
            }
        }
        else
        {
            // ヒットしたのがPlayerだった時
            if (collision.gameObject.CompareTag("Player"))
            {
                var car = collision.gameObject.GetComponentInParent<CarController>();

                if (car != null)
                {
                    // ヒットしたPlayerに中程度のスタン状態を設定
                    car.SetStun(StunType.Light);
                }
            }

            // エフェクトを再生
            PlayDestroyEffect();
            // ロケットを破壊
            Destroy(gameObject);
        }
    }
}