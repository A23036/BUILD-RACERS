using UnityEngine;

public class RocketRed : MonoBehaviour
{
    // Inspectorで速度を設定できるように public にします
    public float speed = 50f;
    // ロケットが発射されてから自動的に消滅するまでの時間
    public float lifeTime = 100f;

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

    [Header("Homing Settings")]
    public float homingStrength = 5f;   // 追従の強さ
    public float detectDistance = 30f;  // Rayの距離
    public float detectAngle = 30f;     // 斜めRay角度

    private Transform targetPlayer;
    private Transform ownerPlayer; // 発射元

    private bool isLockedOn = false;

    // 生成者をセット
    public void SetOwner(Transform owner)
    {
        ownerPlayer = owner;
    }

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

        // ロックオン処理
        if (!isLockedOn)
        {
            SearchTargetPlayer();   // 見つかるまで毎フレーム
        }
        else
        {
            UpdateHoming();         // ロック後は追従
        }

        // 毎フレーム、currentDirectionに基づいて移動します
        transform.Translate(currentDirection * speed * Time.deltaTime, Space.World);

        // 水平姿勢を維持しつつ、進行方向を向く
        if (currentDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(currentDirection, Vector3.up);
        }
    }

    void UpdateHoming()
    {
        if (targetPlayer == null)
            return;

        Vector3 targetDir =
            (targetPlayer.position - transform.position).normalized;
        
        targetDir.y = 0f; // Y成分を無視（高さを変えない）
        
        currentDirection =
            Vector3.Lerp(
                currentDirection,
                targetDir,
                Time.deltaTime * homingStrength
            ).normalized;
    }

    // ロックオン対象を探す
    void SearchTargetPlayer()
    {
        Vector3 origin = transform.position;

        Vector3[] directions =
        {
        currentDirection,
        Quaternion.Euler(0, detectAngle, 0) * currentDirection,
        Quaternion.Euler(0, -detectAngle, 0) * currentDirection
    };

        foreach (var dir in directions)
        {
            if (Physics.Raycast(origin, dir, out RaycastHit hit, detectDistance))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    Transform hitPlayer = hit.collider.transform.root;

                    if (hitPlayer != ownerPlayer)
                    {
                        targetPlayer = hitPlayer;
                        isLockedOn = true;

                        Debug.Log($"[Rocket] Lock On {hitPlayer.name}");
                        return;
                    }
                }
            }

            Debug.DrawRay(origin, dir * detectDistance, Color.cyan);
        }
    }


    // プレイヤーの方向を向かせる
    void DetectTargetPlayer()
    {
        Vector3 origin = transform.position;

        Vector3[] directions =
        {
        transform.forward,
        Quaternion.Euler(0, detectAngle, 0) * transform.forward,
        Quaternion.Euler(0, -detectAngle, 0) * transform.forward
    };

        foreach (var dir in directions)
        {
            if (Physics.Raycast(origin, dir, out RaycastHit hit, detectDistance))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    Transform hitPlayer = hit.collider.transform.root;

                    // 自分を発射したPlayerは除外
                    if (hitPlayer != ownerPlayer)
                    {
                        targetPlayer = hitPlayer;
                        Debug.Log($"[Rocket] Lock On {hitPlayer.name}");
                        return;
                    }
                }
            }

            Debug.DrawRay(origin, dir * detectDistance, Color.cyan, 2f);
        }
    }

    // 高さ維持処理
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
                Debug.Log("[redrocket]Refrect Limit");
            }
        }
        else if(collision.gameObject.CompareTag("Dirt") || collision.gameObject.CompareTag("Road"))
        {
            return;
        }
        else
        {
            // ヒットしたのがPlayerだった時
            if (collision.gameObject.CompareTag("Player"))
            {
                var car = collision.gameObject.GetComponentInParent<CarController>();

                if (car != null)
                {
                    // ヒットしたPlayerに軽程度のスタン状態を設定
                    car.SetStun(StunType.Light);

                    Debug.Log($"HIT ROCKET : {car.GetName()}");
                }
            }

            // エフェクトを再生
            PlayDestroyEffect();
            // ロケットを破壊
            Destroy(gameObject);

            Debug.Log("[redrocket]not wall");
        }
    }
}